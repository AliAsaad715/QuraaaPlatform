using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Commands.CancelOrder
{
    public record CancelOrderCommand(
        Guid BuyerUserId,
        Guid OrderId,
        string? Reason)
        : IRequest<AppResult<OrderResponse>>;
}
