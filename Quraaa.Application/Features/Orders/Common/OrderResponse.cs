using Quraaa.Domain.Orders.Enums;

namespace Quraaa.Application.Features.Orders.Common
{
    public record OrderResponse(
        Guid OrderId,
        string OrderNumber,
        OrderStatus Status,
        PaymentStatus PaymentStatus,
        string Currency,
        decimal Subtotal,
        decimal ShippingAmount,
        decimal DiscountAmount,
        decimal TotalAmount,
        int ItemCount,
        ShippingLocationResponse? ShippingLocation,
        IReadOnlyCollection<OrderItemResponse> Items,
        DateTime CreationTime,
        DateTime? PaidAtUtc,
        DateTime? CancelledAtUtc,
        string? CancellationReason);
}
