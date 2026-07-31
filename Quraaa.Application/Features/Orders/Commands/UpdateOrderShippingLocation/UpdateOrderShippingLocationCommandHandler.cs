using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Commands.UpdateOrderShippingLocation
{
    public class UpdateOrderShippingLocationCommandHandler
        : BaseApplicationService<UpdateOrderShippingLocationCommandHandler>,
          IRequestHandler<UpdateOrderShippingLocationCommand, AppResult<OrderResponse>>
    {
        private readonly IOrderRepository _orderRepository;

        public UpdateOrderShippingLocationCommandHandler(
            IOrderRepository orderRepository,
            ILogger<UpdateOrderShippingLocationCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
        }

        public async Task<AppResult<OrderResponse>> Handle(
            UpdateOrderShippingLocationCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var order = await _orderRepository.GetByIdForBuyerAsync(
                    request.OrderId,
                    request.BuyerUserId,
                    cancellationToken)
                    ?? throw new NotFoundException("Order not found.");

                order.UpdateShippingLocation(
                    request.Latitude,
                    request.Longitude,
                    request.BuyerUserId);

                await _orderRepository.SaveChangesAsync(cancellationToken);
                return order.ToResponse();
            }, "Order shipping location updated successfully");
        }
    }
}
