using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Orders.Commands.Common;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Orders.Commands.MarkOrderItemFulfilled
{
    public sealed class MarkOrderItemFulfilledCommandHandler
        : BaseApplicationService<MarkOrderItemFulfilledCommandHandler>,
          IRequestHandler<
              MarkOrderItemFulfilledCommand,
              AppResult<SellerOrderItemFulfillmentResponse>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILibraryRepository _libraryRepository;

        public MarkOrderItemFulfilledCommandHandler(
            IOrderRepository orderRepository,
            ILibraryRepository libraryRepository,
            ILogger<MarkOrderItemFulfilledCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<SellerOrderItemFulfillmentResponse>> Handle(
            MarkOrderItemFulfilledCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<
                MarkOrderItemFulfilledCommand,
                SellerOrderItemFulfillmentResponse>(request, async () =>
            {
                var (order, item) =
                    await SellerOrderItemFulfillmentAccess.LoadAsync(
                        _orderRepository,
                        _libraryRepository,
                        request.RequestingUserId,
                        request.OrderId,
                        request.OrderItemId,
                        cancellationToken);

                order.MarkItemFulfilled(item.Id, request.RequestingUserId);
                await _orderRepository.SaveChangesAsync(cancellationToken);

                return SellerOrderItemFulfillmentAccess.ToResponse(order, item);
            }, "Order item marked as fulfilled.");
        }
    }
}
