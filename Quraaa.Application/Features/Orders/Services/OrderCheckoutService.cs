using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Payments.Common;
using Quraaa.Application.Features.Payments.Exceptions;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Domain.Cart;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Services
{
    public sealed class OrderCheckoutService : IOrderCheckoutService
    {
        private static readonly TimeSpan CheckoutLifetime = TimeSpan.FromHours(1);
        private const int MaximumCheckoutLineItems = 100;
        private const int MaximumCheckoutQuantity = 999_999;
        private const long MinimumUsdChargeMinor = 50;
        private const long MaximumUsdAmountMinor = 99_999_999;

        private readonly IPaymentGateway _paymentGateway;
        private readonly IOrderRepository _orderRepository;
        private readonly IListingRepository _listingRepository;

        public OrderCheckoutService(
            IPaymentGateway paymentGateway,
            IOrderRepository orderRepository,
            IListingRepository listingRepository)
        {
            _paymentGateway = paymentGateway;
            _orderRepository = orderRepository;
            _listingRepository = listingRepository;
        }

        public async Task<OrderCheckoutResponse> EnsureCheckoutSessionAsync(
            OrderAggregate order,
            CartAggregate cart,
            string successUrl,
            string cancelUrl,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(
                    order.Currency,
                    _paymentGateway.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException("Order currency does not match the payment gateway currency.");
            }

            ValidateCheckoutAmounts(order);

            var attempt = GetActiveAttempt(order);
            var utcNow = DateTime.UtcNow;
            var attemptHasExpired =
                attempt is not null
                && (!attempt.ExpiresAtUtc.HasValue
                    || attempt.ExpiresAtUtc.Value <= utcNow);

            if (attempt is not null && attemptHasExpired)
            {
                if (attempt.Status == PaymentAttemptStatus.CheckoutCreated
                    && !string.IsNullOrWhiteSpace(attempt.CheckoutSessionId))
                {
                    // Stripe is authoritative once a Session exists. Its
                    // status check prevents a delayed paid webhook from being
                    // overwritten by local timeout compensation.
                    await _paymentGateway.ExpireCheckoutSessionAsync(
                        attempt.CheckoutSessionId,
                        $"expire-stale:{attempt.Id:N}",
                        cancellationToken);
                }

                await ExpireLocalCheckoutAsync(
                    order,
                    cart,
                    attempt,
                    cancellationToken);

                throw new ConflictException(
                    "The payment session expired and the cart was reopened. Create a new order to retry.");
            }

            if (attempt?.Status == PaymentAttemptStatus.CheckoutCreated)
            {
                return order.ToCheckoutResponse(attempt);
            }

            if (attempt is null)
            {
                attempt = order.StartPaymentAttempt(
                    PaymentProvider.Stripe,
                    successUrl,
                    cancelUrl,
                    utcNow.Add(CheckoutLifetime));
            }

            var attemptExpiresAtUtc = attempt.ExpiresAtUtc
                ?? throw new ConflictException(
                    "The payment attempt does not have an expiry.");

            // Commit the inventory reservation, cart lock, order, and stable
            // payment-attempt inputs before making the external Stripe call.
            await _orderRepository.SaveChangesAsync(cancellationToken);

            var metadata = new Dictionary<string, string>
            {
                ["orderId"] = order.Id.ToString(),
                ["paymentAttemptId"] = attempt.Id.ToString(),
                ["buyerUserId"] = order.BuyerUserId.ToString(),
                ["cartId"] = order.SourceCartId.ToString()
            };

            PaymentCheckoutSessionResult checkout;

            try
            {
                // Reusing the same idempotency key and inputs recovers a
                // Session whose successful response was previously lost.
                checkout = await _paymentGateway.CreateCheckoutSessionAsync(
                    order.Items
                        .OrderBy(item => item.Id)
                        .Select(item => new PaymentCheckoutLineItem(
                            item.BookTitleSnapshot,
                            $"{item.BookAuthorSnapshot} · {item.SellerNameSnapshot}",
                            item.UnitPriceMinor,
                            item.Quantity))
                        .ToList(),
                    order.Id.ToString(),
                    metadata,
                    $"checkout:{attempt.Id:N}",
                    attempt.SuccessUrl,
                    attempt.CancelUrl,
                    new DateTimeOffset(
                        DateTime.SpecifyKind(attemptExpiresAtUtc, DateTimeKind.Utc)),
                    cancellationToken);
            }
            catch (PaymentCheckoutExpiryRejectedException)
            {
                // An idempotent replay would have returned any Session Stripe
                // had already created. This rejection therefore proves there
                // is no recoverable external Session for this attempt.
                await ExpireLocalCheckoutAsync(
                    order,
                    cart,
                    attempt,
                    cancellationToken);

                throw new ConflictException(
                    "The payment attempt expired and the cart was reopened. Create a new order to retry.");
            }

            try
            {
                order.AttachCheckoutSession(
                    attempt.Id,
                    checkout.SessionId,
                    checkout.CheckoutUrl,
                    checkout.ExpiresAt.UtcDateTime);

                cart.AttachCheckoutSession(order.Id, checkout.SessionId);
                await _orderRepository.SaveChangesAsync(cancellationToken);
            }
            catch (ConflictException)
            {
                if (await _orderRepository.IsCheckoutSessionAttachedAsync(
                        order.Id,
                        attempt.Id,
                        checkout.SessionId,
                        cancellationToken))
                {
                    // A concurrent retry won the optimistic-concurrency race
                    // after Stripe returned the same idempotent Session. Reuse
                    // that Session; it is not an orphan.
                    return order.ToCheckoutResponse(attempt);
                }

                if (await _orderRepository
                        .IsPaymentAttemptAwaitingCheckoutAttachmentAsync(
                            order.Id,
                            attempt.Id,
                            cancellationToken))
                {
                    // A benign order mutation (for example, a shipping update)
                    // won the row-version race. Keep Stripe's open idempotent
                    // Session; the next request can attach it safely.
                    throw;
                }

                // A cancellation/webhook may win after the durable attempt is
                // written but while Stripe is creating the Session. Close that
                // external Session so a cancelled local order is not payable.
                await _paymentGateway.ExpireCheckoutSessionAsync(
                    checkout.SessionId,
                    $"expire-orphan:{attempt.Id:N}",
                    cancellationToken);
                throw;
            }

            return order.ToCheckoutResponse(attempt);
        }

        private async Task ExpireLocalCheckoutAsync(
            OrderAggregate order,
            CartAggregate cart,
            PaymentAttempt attempt,
            CancellationToken cancellationToken)
        {
            order.MarkExpired(attempt.Id);

            foreach (var item in order.Items.Where(
                item => item.Format == ListingFormat.Physical))
            {
                var listing = await _listingRepository.GetByIdForInventoryAsync(
                    item.ListingId,
                    cancellationToken)
                    ?? throw new NotFoundException("Reserved listing not found.");

                listing.ReleaseReservedStock(item.Quantity, order.BuyerUserId);
            }

            cart.ReopenAfterPaymentFailure(order.Id);
            await _orderRepository.SaveChangesAsync(cancellationToken);
        }

        private static PaymentAttempt? GetActiveAttempt(OrderAggregate order)
        {
            return order.PaymentAttempts
                .Where(attempt => attempt.Status is
                    PaymentAttemptStatus.Created or
                    PaymentAttemptStatus.CheckoutCreated)
                .OrderByDescending(attempt => attempt.AttemptNumber)
                .FirstOrDefault();
        }

        private static void ValidateCheckoutAmounts(OrderAggregate order)
        {
            if (order.Items.Count > MaximumCheckoutLineItems)
            {
                throw new ConflictException(
                    $"Stripe Checkout supports at most {MaximumCheckoutLineItems} order lines.");
            }

            if (order.Items.Any(item =>
                    item.Quantity > MaximumCheckoutQuantity
                    || item.UnitPriceMinor > MaximumUsdAmountMinor))
            {
                throw new ConflictException(
                    "One or more order lines exceed Stripe Checkout limits.");
            }

            var lineItemsTotalMinor = order.Items.Aggregate(
                0L,
                (total, item) => checked(total + item.TotalPriceMinor));

            if (lineItemsTotalMinor != order.TotalAmountMinor)
            {
                throw new ConflictException(
                    "Stripe Checkout does not yet support non-item order adjustments.");
            }

            if (order.TotalAmountMinor is
                < MinimumUsdChargeMinor or
                > MaximumUsdAmountMinor)
            {
                throw new ConflictException(
                    "The USD order total is outside Stripe's supported charge range.");
            }
        }
    }
}
