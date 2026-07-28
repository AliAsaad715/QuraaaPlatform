using Microsoft.Extensions.Options;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Payments.Common;
using Quraaa.Application.Features.Payments.Exceptions;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Domain.Shared.Exceptions;
using Stripe;
using Stripe.Checkout;

namespace Quraaa.Infrastructure.Services
{
    public sealed class StripeOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string Currency { get; set; } = "usd";
        public bool IsTestMode { get; set; } = true;
    }

    public class StripePaymentService : IPaymentGateway, IStripePaymentService
    {
        private static readonly HashSet<string> CheckoutSessionEventTypes = new(StringComparer.Ordinal)
        {
            "checkout.session.completed",
            "checkout.session.async_payment_succeeded",
            "checkout.session.async_payment_failed",
            "checkout.session.expired"
        };

        private readonly StripeOptions _options;
        private readonly SessionService _sessionService;

        public StripePaymentService(
            StripeClient stripeClient,
            IOptions<StripeOptions> options)
        {
            _options = options.Value;
            _sessionService = new SessionService(stripeClient);
        }

        public string Currency =>
            string.IsNullOrWhiteSpace(_options.Currency)
                ? "usd"
                : _options.Currency.Trim().ToLowerInvariant();

        public bool IsTestMode => _options.IsTestMode;

        public async Task<PaymentCheckoutSessionResult> CreateCheckoutSessionAsync(
            IReadOnlyCollection<PaymentCheckoutLineItem> lineItems,
            string clientReferenceId,
            IReadOnlyDictionary<string, string> metadata,
            string idempotencyKey,
            string successUrl,
            string cancelUrl,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(lineItems);
            ArgumentNullException.ThrowIfNull(metadata);

            if (lineItems.Count == 0)
            {
                throw new ArgumentException("At least one checkout line item is required.", nameof(lineItems));
            }

            if (lineItems.Count > 100)
            {
                throw new ArgumentException(
                    "Stripe Checkout accepts at most 100 payment-mode line items.",
                    nameof(lineItems));
            }

            if (lineItems.Any(item =>
                    item.UnitAmountMinor is <= 0 or > 99_999_999
                    || item.Quantity is <= 0 or > 999_999))
            {
                throw new ArgumentException(
                    "Checkout line-item amounts or quantities are outside Stripe limits.",
                    nameof(lineItems));
            }

            var providerMetadata = metadata.ToDictionary(pair => pair.Key, pair => pair.Value);
            var options = new SessionCreateOptions
            {
                Mode = "payment",
                PaymentMethodTypes = ["card"],
                ClientReferenceId = clientReferenceId,
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ExpiresAt = expiresAt.UtcDateTime,
                LineItems = lineItems.Select(item => new SessionLineItemOptions
                {
                    Quantity = item.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currency,
                        UnitAmount = item.UnitAmountMinor,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Name,
                            Description = item.Description
                        }
                    }
                }).ToList(),
                Metadata = providerMetadata,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>(providerMetadata)
                }
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            Session session;

            try
            {
                session = await _sessionService.CreateAsync(
                    options,
                    requestOptions,
                    cancellationToken);
            }
            catch (StripeException exception)
                when (string.Equals(
                        exception.StripeError?.Type,
                        "invalid_request_error",
                        StringComparison.Ordinal)
                    && string.Equals(
                        exception.StripeError?.Param,
                        "expires_at",
                        StringComparison.Ordinal))
            {
                throw new PaymentCheckoutExpiryRejectedException(exception);
            }

            return new PaymentCheckoutSessionResult(
                session.Id,
                session.Url ?? string.Empty,
                new DateTimeOffset(DateTime.SpecifyKind(session.ExpiresAt, DateTimeKind.Utc)));
        }

        public async Task ExpireCheckoutSessionAsync(
            string sessionId,
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            var session = await _sessionService.GetAsync(
                sessionId,
                cancellationToken: cancellationToken);

            if (string.Equals(session.Status, "expired", StringComparison.Ordinal))
            {
                return;
            }

            if (!string.Equals(session.Status, "open", StringComparison.Ordinal))
            {
                throw new ConflictException(
                    "The Stripe checkout session is already complete and cannot be cancelled.");
            }

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = idempotencyKey
            };

            try
            {
                await _sessionService.ExpireAsync(
                    sessionId,
                    new SessionExpireOptions(),
                    requestOptions,
                    cancellationToken);
            }
            catch (StripeException)
            {
                // Resolve a race with Stripe completing/expiring the Session
                // between the status read and the expire request.
                var currentSession = await _sessionService.GetAsync(
                    sessionId,
                    cancellationToken: cancellationToken);

                if (string.Equals(
                        currentSession.Status,
                        "expired",
                        StringComparison.Ordinal))
                {
                    return;
                }

                if (!string.Equals(
                        currentSession.Status,
                        "open",
                        StringComparison.Ordinal))
                {
                    throw new ConflictException(
                        "The Stripe checkout session is already complete and cannot be cancelled.");
                }

                throw;
            }
        }

        public Task<PaymentWebhookEventData> ParseWebhookEventAsync(
            string rawPayload,
            string signatureHeader,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(rawPayload))
            {
                throw new PaymentWebhookVerificationException("The payment webhook payload is empty.");
            }

            if (string.IsNullOrWhiteSpace(signatureHeader))
            {
                throw new PaymentWebhookVerificationException("The payment webhook signature is missing.");
            }

            if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                throw new InvalidOperationException("Stripe:WebhookSecret is not configured.");
            }

            Event stripeEvent;

            try
            {
                // We consume only stable Checkout Session fields and validate
                // amount, currency, mode, identifiers, and metadata ourselves.
                // Allow existing Stripe endpoints pinned to another API
                // version to continue delivering signed events.
                stripeEvent = EventUtility.ConstructEvent(
                    rawPayload,
                    signatureHeader,
                    _options.WebhookSecret.Trim(),
                    throwOnApiVersionMismatch: false);
            }
            catch (StripeException exception)
            {
                throw new PaymentWebhookVerificationException(
                    "The payment webhook signature or payload is invalid.",
                    exception);
            }
            catch (Exception exception)
            {
                throw new PaymentWebhookVerificationException(
                    "The payment webhook payload is malformed.",
                    exception);
            }

            if (!CheckoutSessionEventTypes.Contains(stripeEvent.Type))
            {
                return Task.FromResult(new PaymentWebhookEventData(
                    stripeEvent.Id,
                    stripeEvent.Type,
                    null,
                    null,
                    null,
                    null,
                    null,
                    stripeEvent.Livemode,
                    new Dictionary<string, string>()));
            }

            var session = stripeEvent.Data.Object as Session
                ?? throw new PaymentWebhookVerificationException(
                    "The verified payment webhook did not contain a checkout session.");

            var metadata = session.Metadata?.ToDictionary(pair => pair.Key, pair => pair.Value)
                ?? new Dictionary<string, string>();

            return Task.FromResult(new PaymentWebhookEventData(
                stripeEvent.Id,
                stripeEvent.Type,
                session.Id,
                session.PaymentIntentId,
                session.PaymentStatus,
                session.AmountTotal,
                session.Currency,
                stripeEvent.Livemode,
                metadata));
        }

        public async Task<StripeCheckoutSessionResponse> CreateCheckoutSessionAsync(
            IReadOnlyCollection<StripeCheckoutLineItemRequest> items,
            string successUrl,
            string cancelUrl,
            IReadOnlyDictionary<string, string> metadata,
            CancellationToken cancellationToken = default)
        {
            var clientReferenceId = metadata.TryGetValue("cartId", out var cartId)
                ? cartId
                : metadata.TryGetValue("userId", out var userId)
                    ? userId
                    : "legacy-cart-checkout";

            var session = await CreateCheckoutSessionAsync(
                items.Select(item => new PaymentCheckoutLineItem(
                    item.Name,
                    item.Description,
                    item.UnitAmountMinor,
                    item.Quantity)).ToList(),
                clientReferenceId,
                metadata,
                $"cart-checkout:{clientReferenceId}",
                successUrl,
                cancelUrl,
                DateTimeOffset.UtcNow.AddHours(23),
                cancellationToken);

            return new StripeCheckoutSessionResponse(session.SessionId, session.CheckoutUrl);
        }

        async Task<StripeWebhookEventData> IStripePaymentService.ParseWebhookEventAsync(
            string payload,
            string stripeSignatureHeader,
            CancellationToken cancellationToken)
        {
            var paymentEvent = await ParseWebhookEventAsync(
                payload,
                stripeSignatureHeader,
                cancellationToken);

            return new StripeWebhookEventData(
                paymentEvent.EventType,
                paymentEvent.SessionId ?? string.Empty,
                paymentEvent.PaymentIntentId,
                null,
                paymentEvent.Metadata);
        }
    }
}
