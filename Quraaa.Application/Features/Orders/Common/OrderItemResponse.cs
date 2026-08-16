using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders.Enums;

namespace Quraaa.Application.Features.Orders.Common
{
    public record OrderItemResponse(
        Guid OrderItemId,
        Guid ListingId,
        Guid BookId,
        SellerType SellerType,
        Guid SellerId,
        string SellerName,
        ListingFormat Format,
        BookCondition? Condition,
        string Title,
        string Author,
        string? CoverImageUrl,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        OrderItemFulfillmentStatus FulfillmentStatus);
}
