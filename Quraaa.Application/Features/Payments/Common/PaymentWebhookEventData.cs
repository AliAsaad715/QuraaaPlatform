namespace Quraaa.Application.Features.Payments.Common
{
    public sealed record PaymentWebhookEventData(
        string ProviderEventId,
        string EventType,
        string? SessionId,
        string? PaymentIntentId,
        string? PaymentStatus,
        long? AmountTotalMinor,
        string? Currency,
        bool LiveMode,
        IReadOnlyDictionary<string, string> Metadata);
}
