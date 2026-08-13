namespace Quraaa.API.Requests.Orders
{
    public record UpdateOrderShippingLocationRequest(
        Guid? ShippingLocationId = null,
        double? Latitude = null,
        double? Longitude = null);
}
