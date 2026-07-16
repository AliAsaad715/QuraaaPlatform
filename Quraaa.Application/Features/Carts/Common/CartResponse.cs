using Quraaa.Domain.Cart;
using Quraaa.Domain.Cart.Enums;

namespace Quraaa.Application.Features.Carts.Common
{
    public record CartResponse(
        Guid? CartId,
        CartStatus Status,
        IReadOnlyCollection<CartItemResponse> Items,
        decimal TotalAmount,
        int ItemCount,
        string? StripeCheckoutSessionId
    )
    {
        public static CartResponse FromCart(CartAggregate cart)
        {
            var items = cart.Items
                .Select(x => new CartItemResponse(
                    x.ListingId,
                    x.Quantity,
                    x.UnitPriceSnapshot,
                    x.Quantity * x.UnitPriceSnapshot))
                .ToList();

            return new CartResponse(
                cart.Id,
                cart.Status,
                items,
                cart.TotalAmount,
                items.Sum(x => x.Quantity),
                cart.StripeCheckoutSessionId);
        }

        public static CartResponse Empty()
        {
            return new CartResponse(null, CartStatus.Active, Array.Empty<CartItemResponse>(), 0m, 0, null);
        }
    }
}
