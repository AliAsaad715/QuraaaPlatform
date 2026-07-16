using Quraaa.Application.Features.Carts.Common;

namespace Quraaa.Application.Features.Carts.Interfaces
{
    public interface IStripePaymentService
    {
        Task<StripeCheckoutSessionResponse> CreateCheckoutSessionAsync(
            IReadOnlyCollection<StripeCheckoutLineItemRequest> items,
            string successUrl,
            string cancelUrl,
            IReadOnlyDictionary<string, string> metadata,
            CancellationToken cancellationToken = default);

        Task<StripeWebhookEventData> ParseWebhookEventAsync(
            string payload,
            string stripeSignatureHeader,
            CancellationToken cancellationToken = default);
    }
}
