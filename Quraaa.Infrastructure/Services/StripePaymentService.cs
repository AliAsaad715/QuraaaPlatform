using Microsoft.Extensions.Options;
using Quraaa.Application.Features.Carts.Common;
using Quraaa.Application.Features.Carts.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace Quraaa.Infrastructure.Services
{
    public sealed class StripeOptions
    {
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
        public string Currency { get; set; } = "usd";
    }

    public class StripePaymentService : IStripePaymentService
    {
        private readonly StripeOptions _options;

        public StripePaymentService(IOptions<StripeOptions> options)
        {
            _options = options.Value;
            StripeConfiguration.ApiKey = _options.SecretKey;
        }

        public async Task<StripeCheckoutSessionResponse> CreateCheckoutSessionAsync(
            IReadOnlyCollection<StripeCheckoutLineItemRequest> items,
            string successUrl,
            string cancelUrl,
            IReadOnlyDictionary<string, string> metadata,
            CancellationToken cancellationToken = default)
        {
            var sessionService = new SessionService();

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                LineItems = items.Select(x => new SessionLineItemOptions
                {
                    Quantity = x.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = string.IsNullOrWhiteSpace(x.Currency) ? _options.Currency : x.Currency,
                        UnitAmount = x.UnitAmountMinor,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = x.Name,
                            Description = x.Description
                        }
                    }
                }).ToList(),
                Metadata = metadata.ToDictionary(x => x.Key, x => x.Value)
            };

            var session = await sessionService.CreateAsync(options, cancellationToken: cancellationToken);

            return new StripeCheckoutSessionResponse(session.Id, session.Url ?? string.Empty);
        }

        public Task<StripeWebhookEventData> ParseWebhookEventAsync(
            string payload,
            string stripeSignatureHeader,
            CancellationToken cancellationToken = default)
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, stripeSignatureHeader, _options.WebhookSecret, throwOnApiVersionMismatch: false);

            if (!string.Equals(stripeEvent.Type, "checkout.session.completed", StringComparison.Ordinal))
            {
                return Task.FromResult(new StripeWebhookEventData(
                    stripeEvent.Type,
                    string.Empty,
                    null,
                    null,
                    new Dictionary<string, string>()));
            }

            var session = stripeEvent.Data.Object as Session
                ?? throw new InvalidOperationException("Invalid Stripe checkout session payload.");

            var metadata = session.Metadata?.ToDictionary(x => x.Key, x => x.Value)
                ?? new Dictionary<string, string>();

            return Task.FromResult(new StripeWebhookEventData(
                stripeEvent.Type,
                session.Id,
                session.PaymentIntentId,
                session.CustomerId,
                metadata));
        }
    }
}
