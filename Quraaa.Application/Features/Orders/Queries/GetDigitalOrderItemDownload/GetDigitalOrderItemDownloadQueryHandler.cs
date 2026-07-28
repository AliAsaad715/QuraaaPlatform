using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Queries.GetDigitalOrderItemDownload
{
    public class GetDigitalOrderItemDownloadQueryHandler
        : BaseApplicationService<GetDigitalOrderItemDownloadQueryHandler>,
          IRequestHandler<GetDigitalOrderItemDownloadQuery, AppResult<DigitalOrderItemDownloadResponse>>
    {
        private readonly IOrderRepository _orderRepository;

        public GetDigitalOrderItemDownloadQueryHandler(
            IOrderRepository orderRepository,
            ILogger<GetDigitalOrderItemDownloadQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
        }

        public async Task<AppResult<DigitalOrderItemDownloadResponse>> Handle(
            GetDigitalOrderItemDownloadQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var order = await _orderRepository.GetByIdForBuyerIncludingArchivedAsync(
                    request.OrderId,
                    request.BuyerUserId,
                    cancellationToken)
                    ?? throw new NotFoundException("Order not found.");

                if (order.PaymentStatus != PaymentStatus.Paid)
                {
                    throw new ConflictException("The order must be paid before downloading digital items.");
                }

                var item = order.Items.FirstOrDefault(item => item.Id == request.OrderItemId)
                    ?? throw new NotFoundException("Order item not found.");

                if (item.Format != ListingFormat.Digital
                    || string.IsNullOrWhiteSpace(item.DigitalAssetUrlSnapshot))
                {
                    throw new NotFoundException("Digital asset not found.");
                }

                return new DigitalOrderItemDownloadResponse(
                    order.Id,
                    item.Id,
                    item.DigitalAssetUrlSnapshot);
            }, "Digital order item download authorized");
        }
    }
}
