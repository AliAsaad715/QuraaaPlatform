using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Orders.Enums;

namespace Quraaa.Application.Features.Orders.Queries.GetMyOrders
{
    public record GetMyOrdersQuery(
        Guid BuyerUserId,
        int PageNumber,
        int PageSize,
        OrderStatus? Status)
        : IRequest<AppResult<PagedResult<OrderSummaryResponse>>>;
}
