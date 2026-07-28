using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Commands.ArchiveOrder
{
    public class ArchiveOrderCommandHandler
        : BaseApplicationService<ArchiveOrderCommandHandler>,
          IRequestHandler<ArchiveOrderCommand, AppResult>
    {
        private readonly IOrderRepository _orderRepository;

        public ArchiveOrderCommandHandler(
            IOrderRepository orderRepository,
            ILogger<ArchiveOrderCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
        }

        public async Task<AppResult> Handle(
            ArchiveOrderCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var order = await _orderRepository.GetByIdForBuyerAsync(
                    request.OrderId,
                    request.BuyerUserId,
                    cancellationToken)
                    ?? throw new NotFoundException("Order not found.");

                order.Archive(request.BuyerUserId);
                await _orderRepository.SaveChangesAsync(cancellationToken);
            }, "Order archived successfully");
        }
    }
}
