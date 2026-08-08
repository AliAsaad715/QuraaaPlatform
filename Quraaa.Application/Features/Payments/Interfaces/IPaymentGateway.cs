using Quraaa.Application.Features.Payments.Common;

namespace Quraaa.Application.Features.Payments.Interfaces
{
    public interface IPaymentGateway
    {
        string Currency { get; }
        bool IsTestMode { get; }

        Task<PaymentCheckoutSessionResult> CreateCheckoutSessionAsync(
            PaymentCheckoutSessionRequest request,
            CancellationToken cancellationToken = default);

        Task<PaymentCheckoutSessionState> GetCheckoutSessionAsync(
            string sessionId,
            CancellationToken cancellationToken = default);

        Task ExpireCheckoutSessionAsync(
            string sessionId,
            string idempotencyKey,
            CancellationToken cancellationToken = default);

        Task<PaymentWebhookEventData> ParseWebhookEventAsync(
            string rawPayload,
            string signatureHeader,
            CancellationToken cancellationToken = default);
    }
}
