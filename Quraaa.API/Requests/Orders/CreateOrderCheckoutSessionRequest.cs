namespace Quraaa.API.Requests.Orders
{
    public record CreateOrderCheckoutSessionRequest(
        string SuccessUrl,
        string CancelUrl);
}
