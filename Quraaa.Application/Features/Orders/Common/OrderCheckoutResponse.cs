namespace Quraaa.Application.Features.Orders.Common
{
    public record OrderCheckoutResponse(
        OrderResponse Order,
        Guid PaymentAttemptId,
        string CheckoutSessionId,
        string CheckoutUrl,
        DateTimeOffset ExpiresAt);
}
