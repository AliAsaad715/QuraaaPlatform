using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders.Enums;

namespace Quraaa.Application.Features.Orders.Common
{
    public sealed record SellerOrderItemResponse(
        Guid OrderId,
        string OrderNumber,
        OrderStatus OrderStatus,
        PaymentStatus PaymentStatus,
        string Currency,
        Guid OrderItemId,
        Guid ListingId,
        Guid BookId,
        string Title,
        string Author,
        string? CoverImageUrl,
        BookCondition? Condition,
        int Quantity,
        decimal UnitPrice,
        decimal LineTotal,
        OrderItemFulfillmentStatus FulfillmentStatus,
        ShippingLocationResponse? ShippingLocation,
        DateTime CreationTime,
        DateTime? PaidAtUtc);
}
