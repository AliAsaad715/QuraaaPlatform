using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Orders.Services;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Commands.CreateOrderCheckoutSession
{
    public class CreateOrderCheckoutSessionCommandHandler
        : BaseApplicationService<CreateOrderCheckoutSessionCommandHandler>,
          IRequestHandler<CreateOrderCheckoutSessionCommand, AppResult<OrderCheckoutResponse>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IOrderCheckoutService _orderCheckoutService;

        public CreateOrderCheckoutSessionCommandHandler(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IOrderCheckoutService orderCheckoutService,
            ILogger<CreateOrderCheckoutSessionCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _orderCheckoutService = orderCheckoutService;
        }

        public async Task<AppResult<OrderCheckoutResponse>> Handle(
            CreateOrderCheckoutSessionCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var order = await _orderRepository.GetByIdForBuyerAsync(
                    request.OrderId,
                    request.BuyerUserId,
                    cancellationToken)
                    ?? throw new NotFoundException("Order not found.");

                var cart = await _cartRepository.GetByIdAsync(
                    order.SourceCartId,
                    cancellationToken)
                    ?? throw new NotFoundException("Source cart not found.");

                return await _orderCheckoutService.EnsureCheckoutSessionAsync(
                    order,
                    cart,
                    request.SuccessUrl,
                    request.CancelUrl,
                    cancellationToken);
            }, "Stripe checkout session retrieved successfully");
        }
    }
}
