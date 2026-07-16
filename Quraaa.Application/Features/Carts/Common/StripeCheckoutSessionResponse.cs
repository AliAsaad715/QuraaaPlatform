namespace Quraaa.Application.Features.Carts.Common
{
    public record StripeCheckoutSessionResponse(
        string SessionId,
        string Url
    );
}
