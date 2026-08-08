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
        double? ShippingLongitude)
        : IRequest<AppResult<OrderCheckoutResponse>>;
}
