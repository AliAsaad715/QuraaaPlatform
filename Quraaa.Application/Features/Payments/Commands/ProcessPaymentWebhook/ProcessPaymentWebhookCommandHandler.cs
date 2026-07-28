using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Payments.Common;
using Quraaa.Application.Features.Payments.Exceptions;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Purchases;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Payments.Commands.ProcessPaymentWebhook
{
    public class ProcessPaymentWebhookCommandHandler
        : BaseApplicationService<ProcessPaymentWebhookCommandHandler>,
          IRequestHandler<ProcessPaymentWebhookCommand, AppResult>
    {
        private const string ProviderName = "stripe";

        private readonly IPaymentGateway _paymentGateway;
        private readonly IPaymentEventInbox _paymentEventInbox;
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IBookPurchaseRepository _bookPurchaseRepository;

        public ProcessPaymentWebhookCommandHandler(
            IPaymentGateway paymentGateway,
            IPaymentEventInbox paymentEventInbox,
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IListingRepository listingRepository,
            IBookPurchaseRepository bookPurchaseRepository,
            ILogger<ProcessPaymentWebhookCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _paymentGateway = paymentGateway;
            _paymentEventInbox = paymentEventInbox;
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _listingRepository = listingRepository;
            _bookPurchaseRepository = bookPurchaseRepository;
        }

        public async Task<AppResult> Handle(
            ProcessPaymentWebhookCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var paymentEvent = await _paymentGateway.ParseWebhookEventAsync(
                    request.Payload,
                    request.SignatureHeader,
                    cancellationToken);

                if (await _paymentEventInbox.ExistsAsync(
                        ProviderName,
                        paymentEvent.ProviderEventId,
                        cancellationToken))
                {
                    return;
                }

                if (!IsCheckoutSessionEvent(paymentEvent.EventType))
                {
                    await _paymentEventInbox.AddAsync(
                        ProviderName,
                        paymentEvent.ProviderEventId,
                        paymentEvent.EventType,
                        cancellationToken: cancellationToken);

                    await SaveWebhookChangesAsync(
                        paymentEvent.ProviderEventId,
                        cancellationToken);
                    return;
                }

                var order = await ResolveOrderAsync(paymentEvent, cancellationToken);
                var attempt = order is null ? null : ResolveAttempt(order, paymentEvent);

                if (order is null)
                {
                    if (await TryProcessLegacyCartEventAsync(
                            paymentEvent,
                            cancellationToken))
                    {
                        await _paymentEventInbox.AddAsync(
                            ProviderName,
                            paymentEvent.ProviderEventId,
                            paymentEvent.EventType,
                            cancellationToken: cancellationToken);

                        await SaveWebhookChangesAsync(
                            paymentEvent.ProviderEventId,
                            cancellationToken);
                        return;
                    }

                    Logger.LogWarning(
                        "Verified Stripe event {EventId} could not be correlated to a local order or legacy cart.",
                        paymentEvent.ProviderEventId);

                    // Do not poison the inbox. A non-2xx response lets Stripe
                    // retry after a transient attach/cutover race is resolved.
                    throw new NotFoundException(
                        "The Stripe checkout session is not correlated yet.");
                }

                if (attempt is null)
                {
                    Logger.LogWarning(
                        "Verified Stripe event {EventId} resolved order {OrderId} but no payment attempt.",
                        paymentEvent.ProviderEventId,
                        order.Id);

                    throw new NotFoundException(
                        "The Stripe payment attempt is not correlated yet.");
                }

                EnsureProviderModeMatches(paymentEvent);
                EnsureSessionMatchesAttempt(paymentEvent, attempt);

                await _paymentEventInbox.AddAsync(
                    ProviderName,
                    paymentEvent.ProviderEventId,
                    paymentEvent.EventType,
                    order.Id,
                    attempt.Id,
                    cancellationToken);

                switch (paymentEvent.EventType)
                {
                    case "checkout.session.completed"
                        when string.Equals(
                            paymentEvent.PaymentStatus,
                            "paid",
                            StringComparison.OrdinalIgnoreCase):
                    case "checkout.session.async_payment_succeeded":
                        await CompletePaidOrderAsync(
                            order,
                            attempt,
                            paymentEvent,
                            cancellationToken);
                        break;

                    case "checkout.session.async_payment_failed":
                        if (order.Status == OrderStatus.Pending
                            && order.PaymentStatus != PaymentStatus.Paid
                            && attempt.Status is
                                PaymentAttemptStatus.Created or
                                PaymentAttemptStatus.CheckoutCreated)
                        {
                            order.MarkPaymentFailed(
                                attempt.Id,
                                "async_payment_failed",
                                "Stripe reported that the asynchronous payment failed.");

                            await ReleaseInventoryAndReopenCartAsync(
                                order,
                                cancellationToken);
                        }
                        break;

                    case "checkout.session.expired":
                        await ExpireOrderAsync(
                            order,
                            attempt,
                            cancellationToken);
                        break;
                }

                await SaveWebhookChangesAsync(
                    paymentEvent.ProviderEventId,
                    cancellationToken);
            }, "Stripe payment webhook processed successfully");
        }

        private async Task SaveWebhookChangesAsync(
            string providerEventId,
            CancellationToken cancellationToken)
        {
            try
            {
                await _orderRepository.SaveChangesAsync(cancellationToken);
            }
            catch (PaymentEventAlreadyProcessedException)
            {
                Logger.LogInformation(
                    "Stripe event {EventId} was claimed concurrently and is already processed.",
                    providerEventId);
            }
        }

        private async Task<OrderAggregate?> ResolveOrderAsync(
            PaymentWebhookEventData paymentEvent,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(paymentEvent.SessionId))
            {
                var bySession = await _orderRepository.GetByCheckoutSessionIdAsync(
                    paymentEvent.SessionId,
                    cancellationToken);

                if (bySession is not null)
                {
                    return bySession;
                }
            }

            if (TryGetMetadataGuid(paymentEvent, "orderId", out var orderId))
            {
                return await _orderRepository.GetByIdAsync(
                    orderId,
                    cancellationToken);
            }

            return null;
        }

        private static PaymentAttempt? ResolveAttempt(
            OrderAggregate order,
            PaymentWebhookEventData paymentEvent)
        {
            if (TryGetMetadataGuid(
                    paymentEvent,
                    "paymentAttemptId",
                    out var paymentAttemptId))
            {
                return order.PaymentAttempts.FirstOrDefault(
                    attempt => attempt.Id == paymentAttemptId);
            }

            if (!string.IsNullOrWhiteSpace(paymentEvent.SessionId))
            {
                return order.PaymentAttempts.FirstOrDefault(
                    attempt => string.Equals(
                        attempt.CheckoutSessionId,
                        paymentEvent.SessionId,
                        StringComparison.Ordinal));
            }

            return null;
        }

        private async Task CompletePaidOrderAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            PaymentWebhookEventData paymentEvent,
            CancellationToken cancellationToken)
        {
            EnsurePaidAmountMatches(order, attempt, paymentEvent);

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                order.MarkPaid(attempt.Id, paymentEvent.PaymentIntentId);
                return;
            }

            order.MarkPaid(attempt.Id, paymentEvent.PaymentIntentId);

            var cart = await _cartRepository.GetByIdAsync(
                order.SourceCartId,
                cancellationToken);

            if (cart is null)
            {
                Logger.LogError(
                    "Paid order {OrderId} has no mutable source cart {CartId}; payment recognition will continue.",
                    order.Id,
                    order.SourceCartId);
            }
            else
            {
                try
                {
                    cart.MarkPaid(order.Id, paymentEvent.PaymentIntentId);
                }
                catch (ConflictException exception)
                {
                    Logger.LogError(
                        exception,
                        "Paid order {OrderId} could not finalize source cart {CartId}; payment recognition will continue.",
                        order.Id,
                        order.SourceCartId);
                }
            }

            var purchases = order.Items.Select(item =>
                BookPurchaseAggregate.Create(
                    order.BuyerUserId,
                    item.BookId,
                    item.ListingId,
                    item.Quantity,
                    item.UnitPriceMinor / 100m,
                    order.Id,
                    item.Id));

            await _bookPurchaseRepository.AddRangeAsync(
                purchases,
                cancellationToken);
        }

        private async Task ExpireOrderAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            CancellationToken cancellationToken)
        {
            if (order.PaymentStatus == PaymentStatus.Paid
                || order.Status is OrderStatus.Cancelled or OrderStatus.Expired
                || attempt.Status is not (
                    PaymentAttemptStatus.Created or
                    PaymentAttemptStatus.CheckoutCreated))
            {
                return;
            }

            order.MarkExpired(attempt.Id);

            await ReleaseInventoryAndReopenCartAsync(order, cancellationToken);
        }

        private async Task ReleaseInventoryAndReopenCartAsync(
            OrderAggregate order,
            CancellationToken cancellationToken)
        {
            foreach (var item in order.Items.Where(
                item => item.Format == ListingFormat.Physical))
            {
                var listing = await _listingRepository.GetByIdForInventoryAsync(
                    item.ListingId,
                    cancellationToken)
                    ?? throw new NotFoundException("Reserved listing not found.");

                listing.ReleaseReservedStock(item.Quantity, order.BuyerUserId);
            }

            var cart = await _cartRepository.GetByIdAsync(
                order.SourceCartId,
                cancellationToken);

            if (cart is null)
            {
                Logger.LogError(
                    "Terminal unpaid order {OrderId} has no mutable source cart {CartId}; inventory compensation will continue.",
                    order.Id,
                    order.SourceCartId);
                return;
            }

            try
            {
                cart.ReopenAfterPaymentFailure(order.Id);
            }
            catch (ConflictException exception)
            {
                Logger.LogError(
                    exception,
                    "Terminal unpaid order {OrderId} could not reopen source cart {CartId}; inventory compensation will continue.",
                    order.Id,
                    order.SourceCartId);
            }
        }

        private async Task<bool> TryProcessLegacyCartEventAsync(
            PaymentWebhookEventData paymentEvent,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(paymentEvent.SessionId)
                || !TryGetMetadataGuid(paymentEvent, "cartId", out var cartId)
                || !TryGetMetadataGuid(paymentEvent, "userId", out var userId))
            {
                return false;
            }

            var cart = await _cartRepository.GetByStripeSessionIdAsync(
                paymentEvent.SessionId,
                cancellationToken);

            if (cart is null
                || cart.Id != cartId
                || cart.UserId != userId
                || cart.PendingOrderId.HasValue)
            {
                return false;
            }

            EnsureProviderModeMatches(paymentEvent);

            var isPaidEvent =
                paymentEvent.EventType == "checkout.session.async_payment_succeeded"
                || (paymentEvent.EventType == "checkout.session.completed"
                    && string.Equals(
                        paymentEvent.PaymentStatus,
                        "paid",
                        StringComparison.OrdinalIgnoreCase));

            if (isPaidEvent)
            {
                EnsureLegacyPaidAmountMatches(cart, paymentEvent);

                if (cart.Status == CartStatus.Paid)
                {
                    if (!string.IsNullOrWhiteSpace(cart.StripePaymentIntentId)
                        && !string.Equals(
                            cart.StripePaymentIntentId,
                            paymentEvent.PaymentIntentId,
                            StringComparison.Ordinal))
                    {
                        throw new ConflictException(
                            "A different payment intent already paid this legacy cart.");
                    }

                    return true;
                }

                if (cart.Status != CartStatus.PendingPayment)
                {
                    return false;
                }

                var purchases = new List<BookPurchaseAggregate>(cart.Items.Count);

                foreach (var cartItem in cart.Items)
                {
                    var listing = await _listingRepository.GetByIdForInventoryAsync(
                        cartItem.ListingId,
                        cancellationToken)
                        ?? throw new NotFoundException(
                            "A paid legacy cart references a missing listing.");

                    purchases.Add(BookPurchaseAggregate.Create(
                        cart.UserId,
                        listing.BookId,
                        listing.Id,
                        cartItem.Quantity,
                        cartItem.UnitPriceSnapshot));

                    if (listing.Format != ListingFormat.Physical)
                    {
                        continue;
                    }

                    if (listing.Stock is >= 0
                        && listing.Stock.Value >= cartItem.Quantity)
                    {
                        listing.ReserveStock(cartItem.Quantity, cart.UserId);
                    }
                    else
                    {
                        // The legacy flow did not reserve inventory before
                        // redirecting to Stripe. Preserve the captured payment
                        // fact and flag this item for manual reconciliation.
                        Logger.LogCritical(
                            "Paid legacy cart {CartId} oversold listing {ListingId}; required {Quantity}, available {Stock}.",
                            cart.Id,
                            listing.Id,
                            cartItem.Quantity,
                            listing.Stock);
                    }
                }

                await _bookPurchaseRepository.AddRangeAsync(
                    purchases,
                    cancellationToken);

                cart.MarkPaid(paymentEvent.PaymentIntentId);
                return true;
            }

            if (paymentEvent.EventType is
                    "checkout.session.async_payment_failed" or
                    "checkout.session.expired"
                && cart.Status == CartStatus.PendingPayment)
            {
                cart.ReopenAfterLegacyPaymentFailure(paymentEvent.SessionId);
            }

            // checkout.session.completed with payment_status=unpaid is
            // intentionally only acknowledged; the later async event is
            // authoritative.
            return true;
        }

        private void EnsureLegacyPaidAmountMatches(
            Quraaa.Domain.Cart.CartAggregate cart,
            PaymentWebhookEventData paymentEvent)
        {
            long expectedAmountMinor = 0;

            foreach (var item in cart.Items)
            {
                var unitAmountMinor = checked((long)decimal.Round(
                    item.UnitPriceSnapshot * 100m,
                    0,
                    MidpointRounding.AwayFromZero));

                expectedAmountMinor = checked(
                    expectedAmountMinor + checked(unitAmountMinor * item.Quantity));
            }

            if (!paymentEvent.AmountTotalMinor.HasValue
                || paymentEvent.AmountTotalMinor.Value != expectedAmountMinor)
            {
                throw new DomainException(
                    "Stripe paid amount does not match the legacy cart.");
            }

            if (!string.Equals(
                    paymentEvent.Currency,
                    _paymentGateway.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException(
                    "Stripe payment currency does not match the configured currency.");
            }

            if (string.IsNullOrWhiteSpace(paymentEvent.PaymentIntentId))
            {
                throw new DomainException(
                    "Stripe did not provide a payment intent for the paid legacy cart.");
            }
        }

        private void EnsureProviderModeMatches(PaymentWebhookEventData paymentEvent)
        {
            var modeMismatch = _paymentGateway.IsTestMode
                ? paymentEvent.LiveMode
                : !paymentEvent.LiveMode;

            if (modeMismatch)
            {
                throw new DomainException(
                    "Stripe webhook mode does not match the configured payment mode.");
            }
        }

        private void EnsurePaidAmountMatches(
            OrderAggregate order,
            PaymentAttempt attempt,
            PaymentWebhookEventData paymentEvent)
        {
            if (!paymentEvent.AmountTotalMinor.HasValue
                || paymentEvent.AmountTotalMinor.Value != order.TotalAmountMinor
                || paymentEvent.AmountTotalMinor.Value != attempt.AmountMinor)
            {
                throw new DomainException(
                    "Stripe paid amount does not match the local order.");
            }

            if (string.IsNullOrWhiteSpace(paymentEvent.Currency)
                || !string.Equals(
                    paymentEvent.Currency,
                    order.Currency,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    paymentEvent.Currency,
                    attempt.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException(
                    "Stripe payment currency does not match the local order.");
            }

            if (string.IsNullOrWhiteSpace(paymentEvent.PaymentIntentId))
            {
                throw new DomainException(
                    "Stripe did not provide a payment intent for the paid order.");
            }
        }

        private static void EnsureSessionMatchesAttempt(
            PaymentWebhookEventData paymentEvent,
            PaymentAttempt attempt)
        {
            if (!string.IsNullOrWhiteSpace(attempt.CheckoutSessionId)
                && !string.IsNullOrWhiteSpace(paymentEvent.SessionId)
                && !string.Equals(
                    attempt.CheckoutSessionId,
                    paymentEvent.SessionId,
                    StringComparison.Ordinal))
            {
                throw new DomainException(
                    "Stripe checkout session does not match the local payment attempt.");
            }
        }

        private static bool TryGetMetadataGuid(
            PaymentWebhookEventData paymentEvent,
            string key,
            out Guid value)
        {
            value = Guid.Empty;

            return paymentEvent.Metadata.TryGetValue(key, out var rawValue)
                && Guid.TryParse(rawValue, out value);
        }

        private static bool IsCheckoutSessionEvent(string eventType)
        {
            return eventType.StartsWith(
                "checkout.session.",
                StringComparison.Ordinal);
        }
    }
}
