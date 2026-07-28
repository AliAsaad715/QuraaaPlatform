using Quraaa.Domain.Orders.Enums;

namespace Quraaa.API.Requests.Orders
{
    public record GetMyOrdersRequest
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public OrderStatus? Status { get; init; }
    }
}
