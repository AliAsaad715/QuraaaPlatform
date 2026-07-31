namespace Quraaa.API.Requests.Orders
{
    public record CreateOrderRequest(
        string SuccessUrl,
        string CancelUrl,
        ShippingLocationRequest? ShippingLocation = null);
}
