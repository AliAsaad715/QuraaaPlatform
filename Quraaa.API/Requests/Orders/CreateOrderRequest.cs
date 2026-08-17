namespace Quraaa.API.Requests.Orders
{
    /// <param name="SuccessUrl">
    /// Optional. Omit it and checkout returns to this API's app return page,
    /// which hands off to the mobile app.
    /// </param>
    /// <param name="CancelUrl">Optional; same default as SuccessUrl.</param>
    /// <param name="ShippingLocationId">A saved location to ship to.</param>
    /// <param name="ShippingLocation">Explicit coordinates to ship to.</param>
    public record CreateOrderRequest(
        string? SuccessUrl = null,
        string? CancelUrl = null,
        Guid? ShippingLocationId = null,
        ShippingLocationRequest? ShippingLocation = null);
}
