using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Payments.Common;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Purchases;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Services
{
    public sealed class OrderPaymentFinalizationService
        : IOrderPaymentFinalizationService
    {
        private readonly IPaymentGateway _paymentGateway;
        private readonly ICartRepository _cartRepository;
        private readonly IBookPurchaseRepository _bookPurchaseRepository;
        private readonly ILogger<OrderPaymentFinalizationService> _logger;

        public OrderPaymentFinalizationService(
            IPaymentGateway paymentGateway,
            ICartRepository cartRepository,
            IBookPurchaseRepository bookPurchaseRepository,
            ILogger<OrderPaymentFinalizationService> logger)
        {
            _paymentGateway = paymentGateway;
            _cartRepository = cartRepository;
            _bookPurchaseRepository = bookPurchaseRepository;
            _logger = logger;
        }

        public Task CompletePaidOrderAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            PaymentWebhookEventData paymentEvent,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(paymentEvent.SessionId))
            {
                throw new DomainException(
                    "Stripe did not provide a checkout session for the paid order.");
            }

            return CompletePaidOrderAsync(
                order,
                attempt,
                paymentEvent.SessionId,
                paymentEvent.PaymentIntentId,
                paymentEvent.AmountTotalMinor,
                paymentEvent.Currency,
                paymentEvent.LiveMode,
                cancellationToken);
        }

        public Task CompletePaidOrderAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            PaymentCheckoutSessionState checkoutSession,
            CancellationToken cancellationToken = default)
        {
            if (!string.Equals(
                    checkoutSession.PaymentStatus,
                    "paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException(
                    "Stripe has not confirmed this checkout session as paid.");
            }

            return CompletePaidOrderAsync(
                order,
                attempt,
                checkoutSession.SessionId,
                checkoutSession.PaymentIntentId,
                checkoutSession.AmountTotalMinor,
                checkoutSession.Currency,
                checkoutSession.LiveMode,
                cancellationToken);
        }

        private async Task CompletePaidOrderAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            string sessionId,
            string? paymentIntentId,
            long? amountTotalMinor,
            string? currency,
            bool liveMode,
            CancellationToken cancellationToken)
        {
            EnsureProviderModeMatches(liveMode);
            EnsureSessionMatchesAttempt(sessionId, attempt);
            EnsurePaidAmountMatches(
                order,
                attempt,
                paymentIntentId,
                amountTotalMinor,
                currency);

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                order.MarkPaid(attempt.Id, paymentIntentId);
                return;
            }

            order.MarkPaid(attempt.Id, paymentIntentId);

            var cart = await _cartRepository.GetByIdAsync(
                order.SourceCartId,
                cancellationToken);

            if (cart is null)
            {
                _logger.LogError(
                    "Paid order {OrderId} has no mutable source cart {CartId}; payment recognition will continue.",
                    order.Id,
                    order.SourceCartId);
            }
            else
            {
                try
                {
                    cart.MarkPaid(order.Id, paymentIntentId);
                }
                catch (ConflictException exception)
                {
                    _logger.LogError(
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
                    item.Id,
                    item.DigitalAssetUrlSnapshot));

            await _bookPurchaseRepository.AddRangeAsync(
                purchases,
                cancellationToken);
        }

        private void EnsureProviderModeMatches(bool liveMode)
        {
            var modeMismatch = _paymentGateway.IsTestMode
                ? liveMode
                : !liveMode;

            if (modeMismatch)
            {
                throw new DomainException(
                    "Stripe payment mode does not match the configured payment mode.");
            }
        }

        private static void EnsureSessionMatchesAttempt(
            string sessionId,
            PaymentAttempt attempt)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new DomainException(
                    "Stripe did not provide a checkout session for the paid order.");
            }

            if (!string.IsNullOrWhiteSpace(attempt.CheckoutSessionId)
                && !string.Equals(
                    attempt.CheckoutSessionId,
                    sessionId,
                    StringComparison.Ordinal))
            {
                throw new DomainException(
                    "Stripe checkout session does not match the local payment attempt.");
            }
        }

        private static void EnsurePaidAmountMatches(
            OrderAggregate order,
            PaymentAttempt attempt,
            string? paymentIntentId,
            long? amountTotalMinor,
            string? currency)
        {
            if (!amountTotalMinor.HasValue
                || amountTotalMinor.Value != order.TotalAmountMinor
                || amountTotalMinor.Value != attempt.AmountMinor)
            {
                throw new DomainException(
                    "Stripe paid amount does not match the local order.");
            }

            if (string.IsNullOrWhiteSpace(currency)
                || !string.Equals(
                    currency,
                    order.Currency,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    currency,
                    attempt.Currency,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException(
                    "Stripe payment currency does not match the local order.");
            }

            if (string.IsNullOrWhiteSpace(paymentIntentId))
            {
                throw new DomainException(
                    "Stripe did not provide a payment intent for the paid order.");
            }
        }
    }
}
