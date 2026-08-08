namespace Quraaa.Application.Features.Payments.Common
{
    public sealed record PaymentCheckoutSessionResult(
        string SessionId,
        string CheckoutUrl,
        DateTimeOffset ExpiresAt);
}
