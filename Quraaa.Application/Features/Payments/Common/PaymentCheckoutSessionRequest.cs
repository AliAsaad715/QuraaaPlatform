namespace Quraaa.Application.Features.Payments.Common
{
    public sealed record PaymentCheckoutSessionRequest(
        IReadOnlyCollection<PaymentCheckoutLineItem> LineItems,
        string ClientReferenceId,
        IReadOnlyDictionary<string, string> Metadata,
        string IdempotencyKey,
        string SuccessUrl,
        string CancelUrl,
        DateTimeOffset ExpiresAt);
}
