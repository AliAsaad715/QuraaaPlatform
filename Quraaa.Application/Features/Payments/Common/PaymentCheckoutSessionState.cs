namespace Quraaa.Application.Features.Payments.Common
{
    public sealed record PaymentCheckoutSessionState(
        string SessionId,
        string? CheckoutUrl,
        string? ClientReferenceId,
        DateTimeOffset ExpiresAt,
        string Status,
        string? Mode,
        string? PaymentStatus,
        string? PaymentIntentId,
        long? AmountTotalMinor,
        string? Currency,
        bool LiveMode,
        IReadOnlyDictionary<string, string> Metadata);
}
