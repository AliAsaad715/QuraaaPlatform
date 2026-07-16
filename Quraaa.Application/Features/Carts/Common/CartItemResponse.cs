namespace Quraaa.Application.Features.Carts.Common
{
    public record CartItemResponse(
        Guid ListingId,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal
    );
}
