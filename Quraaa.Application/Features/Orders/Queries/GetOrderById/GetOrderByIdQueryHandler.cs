using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Queries.GetOrderById
{
    public class GetOrderByIdQueryHandler
        : BaseApplicationService<GetOrderByIdQueryHandler>,
          IRequestHandler<GetOrderByIdQuery, AppResult<OrderResponse>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public GetOrderByIdQueryHandler(
            IOrderRepository orderRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<GetOrderByIdQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<OrderResponse>> Handle(
            GetOrderByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var order = await _orderRepository.GetByIdForBuyerAsync(
                    request.OrderId,
                    request.BuyerUserId,
                    cancellationToken)
                    ?? throw new NotFoundException("Order not found.");

                return order.ToResponse(_imageUrlFormatter);
            }, "Order retrieved successfully");
        }
    }
}
