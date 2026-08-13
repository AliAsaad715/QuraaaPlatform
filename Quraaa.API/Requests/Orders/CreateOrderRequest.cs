namespace Quraaa.API.Requests.Orders
{
    public record CreateOrderRequest(
        string SuccessUrl,
        string CancelUrl,
        Guid? ShippingLocationId = null,
        ShippingLocationRequest? ShippingLocation = null);
}
