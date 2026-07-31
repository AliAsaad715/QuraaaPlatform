using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Orders.Enums;

namespace Quraaa.Application.Features.Orders.Queries.GetSellerOrderItems
{
    public sealed record GetSellerOrderItemsQuery(
        Guid RequestingUserId,
        int PageNumber = 1,
        int PageSize = 20,
        OrderItemFulfillmentStatus? FulfillmentStatus = null)
        : IRequest<AppResult<PagedResult<SellerOrderItemResponse>>>;
}
