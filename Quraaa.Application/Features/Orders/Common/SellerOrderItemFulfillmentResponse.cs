using Quraaa.Domain.Orders.Enums;

namespace Quraaa.Application.Features.Orders.Common
{
    public sealed record SellerOrderItemFulfillmentResponse(
        Guid OrderId,
        Guid OrderItemId,
        OrderStatus OrderStatus,
        PaymentStatus PaymentStatus,
        OrderItemFulfillmentStatus FulfillmentStatus,
        DateTime? FulfilledAtUtc,
        DateTime? OrderCompletedAtUtc);
}
