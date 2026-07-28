using Quraaa.Domain.Orders.Enums;

namespace Quraaa.Application.Features.Orders.Common
{
    public record OrderSummaryResponse(
        Guid OrderId,
        string OrderNumber,
        OrderStatus Status,
        PaymentStatus PaymentStatus,
        string Currency,
        decimal TotalAmount,
        int ItemCount,
        DateTime CreationTime,
        DateTime? PaidAtUtc);
}
