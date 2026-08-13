using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
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
        private readonly IUserRepository _userRepository;

        public UpdateOrderShippingLocationCommandHandler(
            IOrderRepository orderRepository,
            IUserRepository userRepository,
            ILogger<UpdateOrderShippingLocationCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
            _userRepository = userRepository;
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

                double latitude;
                double longitude;

                if (request.ShippingLocationId.HasValue)
                {
                    var buyer = await _userRepository.GetUserWithLocationsByIdAsync(
                        request.BuyerUserId,
                        cancellationToken)
                        ?? throw new NotFoundException("Buyer profile not found.");

                    var selectedLocation = buyer.Locations.FirstOrDefault(
                        location => location.Id == request.ShippingLocationId.Value)
                        ?? throw new NotFoundException("Shipping location not found.");

                    latitude = selectedLocation.Latitude;
                    longitude = selectedLocation.Longitude;
                }
                else
                {
                    latitude = request.Latitude!.Value;
                    longitude = request.Longitude!.Value;
                }

                order.UpdateShippingLocation(latitude, longitude, request.BuyerUserId);

                await _orderRepository.SaveChangesAsync(cancellationToken);
                return order.ToResponse();
            }, "Order shipping location updated successfully");
        }
    }
}
