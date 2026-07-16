namespace Quraaa.Application.Features.Carts.Common
{
    public record StripeWebhookEventData(
        string EventType,
        string SessionId,
        string? PaymentIntentId,
        string? CustomerId,
        IReadOnlyDictionary<string, string> Metadata
    );
}
