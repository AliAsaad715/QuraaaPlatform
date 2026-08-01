using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Orders.Queries.GetSellerOrderItems
{
    public sealed class GetSellerOrderItemsQueryHandler
        : BaseApplicationService<GetSellerOrderItemsQueryHandler>,
          IRequestHandler<
              GetSellerOrderItemsQuery,
              AppResult<PagedResult<SellerOrderItemResponse>>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILibraryRepository _libraryRepository;

        public GetSellerOrderItemsQueryHandler(
            IOrderRepository orderRepository,
            ILibraryRepository libraryRepository,
            ILogger<GetSellerOrderItemsQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<PagedResult<SellerOrderItemResponse>>> Handle(
            GetSellerOrderItemsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.RequestingUserId,
                    cancellationToken);

                var (items, totalCount) =
                    await _orderRepository.GetSellerOrderItemsAsync(
                        request.RequestingUserId,
                        library?.Id,
                        request.PageNumber,
                        request.PageSize,
                        request.FulfillmentStatus,
                        cancellationToken);

                return new PagedResult<SellerOrderItemResponse>(
                    items,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Seller order items retrieved successfully");
        }
    }
}
