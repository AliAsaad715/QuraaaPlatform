using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Commands.CreateOrder
{
    public record CreateOrderCommand(
        Guid BuyerUserId,
        string SuccessUrl,
        string CancelUrl,
        double? ShippingLatitude,
        double? ShippingLongitude,
        Guid? ShippingLocationId = null)
        : IRequest<AppResult<OrderCheckoutResponse>>;
}
