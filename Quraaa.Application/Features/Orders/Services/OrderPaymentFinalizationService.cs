using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Payments.Common;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Payouts;
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
        private readonly ISellerPayoutRepository _sellerPayoutRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILogger<OrderPaymentFinalizationService> _logger;

        public OrderPaymentFinalizationService(
            IPaymentGateway paymentGateway,
            ICartRepository cartRepository,
            IBookPurchaseRepository bookPurchaseRepository,
            ISellerPayoutRepository sellerPayoutRepository,
            ILibraryRepository libraryRepository,
            ILogger<OrderPaymentFinalizationService> logger)
        {
            _paymentGateway = paymentGateway;
            _cartRepository = cartRepository;
            _bookPurchaseRepository = bookPurchaseRepository;
            _sellerPayoutRepository = sellerPayoutRepository;
            _libraryRepository = libraryRepository;
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

            await StageSellerPayoutsAsync(order, cancellationToken);
        }

        /// <summary>
        /// Stages one profit-share payout per library seller of the paid order,
        /// using each library's admin-configured profit-share percentage as of
        /// now. The rows commit atomically with the paid transition
        /// (transactional outbox); the payout background service — woken right
        /// after commit — then moves the money to each library's wallet.
        /// </summary>
        private async Task StageSellerPayoutsAsync(
            OrderAggregate order,
            CancellationToken cancellationToken)
        {
            var payouts = new List<SellerPayoutAggregate>();

            // The payment that funds these payouts: transfers draw on this
            // charge directly, so they are accepted while it is still settling.
            var sourcePaymentIntentId = order.PaymentAttempts
                .FirstOrDefault(attempt => attempt.Status == PaymentAttemptStatus.Succeeded)
                ?.PaymentIntentId;

            var libraryShares = order.Items
                .Where(item => item.SellerType == SellerType.Library)
                .GroupBy(item => item.SellerId)
                .Select(group => new
                {
                    LibraryId = group.Key,
                    GrossAmountMinor = group.Sum(item => item.TotalPriceMinor),
                });

            foreach (var share in libraryShares)
            {
                var library = await _libraryRepository.GetByIdAsync(
                    share.LibraryId,
                    cancellationToken);

                if (library is null)
                {
                    // Order items reference libraries by FK, so this is a data
                    // anomaly rather than a business case. Payment recognition
                    // must not fail because of it; the sale is still recorded.
                    _logger.LogError(
                        "Paid order {OrderId} references library {LibraryId}, which could not be loaded; no payout staged for it.",
                        order.Id,
                        share.LibraryId);
                    continue;
                }

                var payout = SellerPayoutAggregate.Create(
                    order.Id,
                    share.LibraryId,
                    order.Currency,
                    share.GrossAmountMinor,
                    library.ProfitSharePercent,
                    sourcePaymentIntentId);

                if (payout.NetAmountMinor == 0)
                {
                    _logger.LogInformation(
                        "Order {OrderId}: the {ProfitSharePercent}% share of {GrossAmountMinor} minor units for library {LibraryId} rounds to zero; recorded as no amount due.",
                        order.Id,
                        library.ProfitSharePercent,
                        share.GrossAmountMinor,
                        share.LibraryId);
                }

                payouts.Add(payout);
            }

            if (payouts.Count > 0)
            {
                await _sellerPayoutRepository.AddRangeAsync(payouts, cancellationToken);
            }
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
