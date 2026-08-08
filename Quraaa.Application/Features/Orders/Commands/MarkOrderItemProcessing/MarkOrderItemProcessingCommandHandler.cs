using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Orders.Commands.Common;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Orders.Commands.MarkOrderItemProcessing
{
    public sealed class MarkOrderItemProcessingCommandHandler
        : BaseApplicationService<MarkOrderItemProcessingCommandHandler>,
          IRequestHandler<
              MarkOrderItemProcessingCommand,
              AppResult<SellerOrderItemFulfillmentResponse>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILibraryRepository _libraryRepository;

        public MarkOrderItemProcessingCommandHandler(
            IOrderRepository orderRepository,
            ILibraryRepository libraryRepository,
            ILogger<MarkOrderItemProcessingCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<SellerOrderItemFulfillmentResponse>> Handle(
            MarkOrderItemProcessingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<
                MarkOrderItemProcessingCommand,
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

                order.MarkItemProcessing(item.Id, request.RequestingUserId);
                await _orderRepository.SaveChangesAsync(cancellationToken);

                return SellerOrderItemFulfillmentAccess.ToResponse(order, item);
            }, "Order item marked as processing.");
        }
    }
}
