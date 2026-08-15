using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Payments.Common;
using Quraaa.Application.Features.Payments.Exceptions;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Services
{
    public sealed class OrderPaymentReconciliationService
        : IOrderPaymentReconciliationService
    {
        // Stripe retains idempotency results for at least 24 hours. Staying
        // below that boundary makes an expires_at rejection conclusive.
        private static readonly TimeSpan IdempotencyRecoverySafetyWindow =
            TimeSpan.FromHours(23);

        private readonly IPaymentGateway _paymentGateway;
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IOrderPaymentFinalizationService _paymentFinalizationService;
        private readonly ISellerPayoutDispatchSignal _payoutDispatchSignal;
        private readonly ILogger<OrderPaymentReconciliationService> _logger;

        public OrderPaymentReconciliationService(
            IPaymentGateway paymentGateway,
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IListingRepository listingRepository,
            IOrderPaymentFinalizationService paymentFinalizationService,
            ISellerPayoutDispatchSignal payoutDispatchSignal,
            ILogger<OrderPaymentReconciliationService> logger)
        {
            _paymentGateway = paymentGateway;
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _listingRepository = listingRepository;
            _paymentFinalizationService = paymentFinalizationService;
            _payoutDispatchSignal = payoutDispatchSignal;
            _logger = logger;
        }

        public async Task<OrderPaymentReconciliationOutcome> ReconcileExpiredAttemptAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            CancellationToken cancellationToken = default)
        {
            if (order.Status != OrderStatus.Pending
                || order.PaymentStatus != PaymentStatus.Pending
                || attempt.Status is not (
                    PaymentAttemptStatus.Created or
                    PaymentAttemptStatus.CheckoutCreated))
            {
                throw new ConflictException(
                    "Only an active payment attempt on a pending order can be reconciled.");
            }

            PaymentCheckoutSessionState checkoutSession;
            var recoveredAttachment = false;

            if (attempt.Status == PaymentAttemptStatus.Created)
            {
                PaymentCheckoutSessionResult recoveredSession;

                try
                {
                    // Stripe returns the original response when this key was
                    // already accepted, even if our first response was lost.
                    recoveredSession = await _paymentGateway.CreateCheckoutSessionAsync(
                        OrderCheckoutSessionRequestFactory.Create(order, attempt),
                        cancellationToken);
                }
                catch (PaymentCheckoutExpiryRejectedException)
                {
                    var expiresAtUtc = attempt.ExpiresAtUtc
                        ?? throw new ConflictException(
                            "The payment attempt does not have an expiry.");
                    var estimatedAttemptCreatedAtUtc = NormalizeUtc(expiresAtUtc)
                        .Subtract(
                            OrderCheckoutSessionRequestFactory.CheckoutLifetime);

                    if (DateTime.UtcNow - estimatedAttemptCreatedAtUtc
                        >= IdempotencyRecoverySafetyWindow)
                    {
                        throw new ConflictException(
                            "This unattached Stripe attempt is too old for safe automatic expiry. Manual payment reconciliation is required.");
                    }

                    // The stable request was not accepted previously. Its now
                    // stale expires_at is therefore safe evidence that there is
                    // no recoverable external Session for this attempt.
                    await ExpireLocalOrderAsync(order, attempt, cancellationToken);
                    return OrderPaymentReconciliationOutcome.Expired;
                }

                // Keep the original local expiry. It is intentionally already
                // historical during recovery, while the Session id and URL are
                // needed for durable correlation and audit.
                order.AttachCheckoutSession(
                    attempt.Id,
                    recoveredSession.SessionId,
                    recoveredSession.CheckoutUrl,
                    expiresAtUtc: null);

                await TryAttachRecoveredSessionToCartAsync(
                    order,
                    recoveredSession.SessionId,
                    cancellationToken);

                recoveredAttachment = true;
                checkoutSession = await _paymentGateway.GetCheckoutSessionAsync(
                    recoveredSession.SessionId,
                    cancellationToken);
            }
            else
            {
                var sessionId = attempt.CheckoutSessionId;

                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    throw new DomainException(
                        "The active checkout attempt has no Stripe session id.");
                }

                checkoutSession = await _paymentGateway.GetCheckoutSessionAsync(
                    sessionId,
                    cancellationToken);
            }

            EnsureSessionMatchesOrder(checkoutSession, order, attempt);

            if (IsPaid(checkoutSession))
            {
                await _paymentFinalizationService.CompletePaidOrderAsync(
                    order,
                    attempt,
                    checkoutSession,
                    cancellationToken);

                await _orderRepository.SaveChangesAsync(cancellationToken);

                // Payouts staged above are now committed: pay the sellers now
                // rather than on the next periodic sweep.
                _payoutDispatchSignal.RequestImmediateProcessing();
                return OrderPaymentReconciliationOutcome.Paid;
            }

            EnsurePaymentIsUnpaid(checkoutSession);

            if (string.Equals(
                    checkoutSession.Status,
                    "open",
                    StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _paymentGateway.ExpireCheckoutSessionAsync(
                        checkoutSession.SessionId,
                        $"reconcile-expired:{attempt.Id:N}",
                        cancellationToken);
                }
                catch (ConflictException)
                {
                    // Stripe may have completed the Session between our read
                    // and expire call. The authoritative re-read below decides.
                }

                checkoutSession = await _paymentGateway.GetCheckoutSessionAsync(
                    checkoutSession.SessionId,
                    cancellationToken);

                EnsureSessionMatchesOrder(checkoutSession, order, attempt);

                if (IsPaid(checkoutSession))
                {
                    await _paymentFinalizationService.CompletePaidOrderAsync(
                        order,
                        attempt,
                        checkoutSession,
                        cancellationToken);

                    await _orderRepository.SaveChangesAsync(cancellationToken);
                    _payoutDispatchSignal.RequestImmediateProcessing();
                    return OrderPaymentReconciliationOutcome.Paid;
                }

                EnsurePaymentIsUnpaid(checkoutSession);
            }

            if (string.Equals(
                    checkoutSession.Status,
                    "expired",
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    checkoutSession.PaymentStatus,
                    "unpaid",
                    StringComparison.OrdinalIgnoreCase))
            {
                await ExpireLocalOrderAsync(order, attempt, cancellationToken);
                return OrderPaymentReconciliationOutcome.Expired;
            }

            if (string.Equals(
                    checkoutSession.Status,
                    "complete",
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    checkoutSession.PaymentStatus,
                    "unpaid",
                    StringComparison.OrdinalIgnoreCase))
            {
                // Delayed payment methods can complete checkout before their
                // final async result. Never release inventory while Stripe can
                // still report success.
                if (recoveredAttachment)
                {
                    await _orderRepository.SaveChangesAsync(cancellationToken);
                }

                return OrderPaymentReconciliationOutcome.AwaitingPayment;
            }

            throw new ConflictException(
                "Stripe checkout state is not terminal; reconciliation will retry.");
        }

        private async Task ExpireLocalOrderAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            CancellationToken cancellationToken)
        {
            // Resolve all dependent rows first, then commit order, cart, and
            // inventory mutations in one DbContext transaction.
            var physicalListings = new List<(ListingAggregate Listing, int Quantity)>();

            foreach (var item in order.Items.Where(
                item => item.Format == ListingFormat.Physical))
            {
                var listing = await _listingRepository.GetByIdForInventoryAsync(
                    item.ListingId,
                    cancellationToken)
                    ?? throw new NotFoundException(
                        $"Reserved listing {item.ListingId} was not found.");

                physicalListings.Add((listing, item.Quantity));
            }

            var cart = await _cartRepository.GetByIdAsync(
                order.SourceCartId,
                cancellationToken);

            order.MarkExpired(attempt.Id);

            foreach (var (listing, quantity) in physicalListings)
            {
                listing.ReleaseReservedStock(quantity, order.BuyerUserId);
            }

            if (cart is not null
                && cart.Status == CartStatus.PendingPayment
                && cart.PendingOrderId == order.Id)
            {
                cart.ReopenAfterPaymentFailure(order.Id);
            }

            await _orderRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task TryAttachRecoveredSessionToCartAsync(
            OrderAggregate order,
            string sessionId,
            CancellationToken cancellationToken)
        {
            var cart = await _cartRepository.GetByIdAsync(
                order.SourceCartId,
                cancellationToken);

            if (cart is not null
                && cart.Status == CartStatus.PendingPayment
                && cart.PendingOrderId == order.Id)
            {
                cart.AttachCheckoutSession(order.Id, sessionId);
                return;
            }

            _logger.LogWarning(
                "Recovered Stripe session {SessionId} for order {OrderId}, but source cart {CartId} is not awaiting that order.",
                sessionId,
                order.Id,
                order.SourceCartId);
        }

        private void EnsureSessionMatchesOrder(
            PaymentCheckoutSessionState checkoutSession,
            OrderAggregate order,
            PaymentAttempt attempt)
        {
            var modeMismatch = _paymentGateway.IsTestMode
                ? checkoutSession.LiveMode
                : !checkoutSession.LiveMode;

            if (modeMismatch)
            {
                throw new DomainException(
                    "Stripe checkout mode does not match the configured payment mode.");
            }

            if (!string.Equals(
                    checkoutSession.Mode,
                    "payment",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException(
                    "Stripe checkout session is not a payment-mode session.");
            }

            if (!string.Equals(
                    checkoutSession.SessionId,
                    attempt.CheckoutSessionId,
                    StringComparison.Ordinal))
            {
                throw new DomainException(
                    "Stripe checkout session does not match the local payment attempt.");
            }

            if (!string.Equals(
                    checkoutSession.ClientReferenceId,
                    order.Id.ToString(),
                    StringComparison.Ordinal))
            {
                throw new DomainException(
                    "Stripe checkout client reference does not match the local order.");
            }

            EnsureMetadataGuid(checkoutSession, "orderId", order.Id);
            EnsureMetadataGuid(checkoutSession, "paymentAttemptId", attempt.Id);
            EnsureMetadataGuid(checkoutSession, "buyerUserId", order.BuyerUserId);
            EnsureMetadataGuid(checkoutSession, "cartId", order.SourceCartId);

            if (checkoutSession.AmountTotalMinor != order.TotalAmountMinor
                || checkoutSession.AmountTotalMinor != attempt.AmountMinor
                || string.IsNullOrWhiteSpace(checkoutSession.Currency)
                || !string.Equals(
                    checkoutSession.Currency,
                    order.Currency,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    checkoutSession.Currency,
                    attempt.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException(
                    "Stripe checkout amount or currency does not match the local order.");
            }
        }

        private static void EnsureMetadataGuid(
            PaymentCheckoutSessionState checkoutSession,
            string key,
            Guid expectedValue)
        {
            if (!checkoutSession.Metadata.TryGetValue(key, out var rawValue)
                || !Guid.TryParse(rawValue, out var actualValue)
                || actualValue != expectedValue)
            {
                throw new DomainException(
                    $"Stripe checkout metadata '{key}' does not match the local order.");
            }
        }

        private static bool IsPaid(
            PaymentCheckoutSessionState checkoutSession)
        {
            return string.Equals(
                    checkoutSession.PaymentStatus,
                    "paid",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsurePaymentIsUnpaid(
            PaymentCheckoutSessionState checkoutSession)
        {
            if (string.Equals(
                    checkoutSession.PaymentStatus,
                    "no_payment_required",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException(
                    "Stripe returned no_payment_required for a positive payment order.");
            }

            if (!string.Equals(
                    checkoutSession.PaymentStatus,
                    "unpaid",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException(
                    "Stripe returned an unknown payment status; manual reconciliation is required.");
            }
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
