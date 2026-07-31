using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Commands.UpdateOrderShippingLocation
{
    public record UpdateOrderShippingLocationCommand(
        Guid BuyerUserId,
        Guid OrderId,
        double Latitude,
        double Longitude)
        : IRequest<AppResult<OrderResponse>>;
}
