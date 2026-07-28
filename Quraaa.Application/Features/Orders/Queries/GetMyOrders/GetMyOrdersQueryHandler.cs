using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Orders.Queries.GetMyOrders
{
    public class GetMyOrdersQueryHandler
        : BaseApplicationService<GetMyOrdersQueryHandler>,
          IRequestHandler<GetMyOrdersQuery, AppResult<PagedResult<OrderSummaryResponse>>>
    {
        private readonly IOrderRepository _orderRepository;

        public GetMyOrdersQueryHandler(
            IOrderRepository orderRepository,
            ILogger<GetMyOrdersQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
        }

        public async Task<AppResult<PagedResult<OrderSummaryResponse>>> Handle(
            GetMyOrdersQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (orders, totalCount) = await _orderRepository.GetBuyerOrdersAsync(
                    request.BuyerUserId,
                    request.PageNumber,
                    request.PageSize,
                    request.Status,
                    cancellationToken);

                return new PagedResult<OrderSummaryResponse>(
                    orders.Select(order => order.ToSummaryResponse()).ToList(),
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Buyer orders retrieved successfully");
        }
    }
}
