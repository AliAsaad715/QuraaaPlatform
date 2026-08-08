using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Queries.GetOrderById
{
    public record GetOrderByIdQuery(
        Guid BuyerUserId,
        Guid OrderId)
        : IRequest<AppResult<OrderResponse>>;
}
