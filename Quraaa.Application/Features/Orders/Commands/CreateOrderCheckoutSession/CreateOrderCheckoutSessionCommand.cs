using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Commands.CreateOrderCheckoutSession
{
    public record CreateOrderCheckoutSessionCommand(
        Guid BuyerUserId,
        Guid OrderId,
        string SuccessUrl,
        string CancelUrl)
        : IRequest<AppResult<OrderCheckoutResponse>>;
}
