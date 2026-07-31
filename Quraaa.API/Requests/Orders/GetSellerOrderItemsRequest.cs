using Quraaa.Domain.Orders.Enums;

namespace Quraaa.API.Requests.Orders
{
    public sealed record GetSellerOrderItemsRequest
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public OrderItemFulfillmentStatus? FulfillmentStatus { get; init; }
    }
}
