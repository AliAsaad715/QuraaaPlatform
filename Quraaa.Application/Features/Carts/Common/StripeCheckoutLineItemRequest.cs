namespace Quraaa.Application.Features.Carts.Common
{
    public record StripeCheckoutLineItemRequest(
        string Name,
        string? Description,
        long UnitAmountMinor,
        string Currency,
        long Quantity
    );
}
