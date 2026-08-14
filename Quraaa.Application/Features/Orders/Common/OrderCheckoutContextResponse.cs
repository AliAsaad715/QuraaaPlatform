namespace Quraaa.Application.Features.Orders.Common;

public sealed record OrderCheckoutContextResponse(
    bool RequiresShippingLocation,
    Guid? SelectedShippingLocationId,
    IReadOnlyCollection<CheckoutLocationResponse> Locations)
{
    public static OrderCheckoutContextResponse WithoutShipping() =>
        new(false, null, Array.Empty<CheckoutLocationResponse>());
}

public sealed record CheckoutLocationResponse(
    Guid Id,
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    bool IsDefault);
