using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Purchases.Queries.GetBuyHistory
{
    public class GetBuyHistoryQueryHandler
        : BaseApplicationService<GetBuyHistoryQueryHandler>,
          IRequestHandler<GetBuyHistoryQuery, AppResult<PagedResult<BuyHistoryItemResponse>>>
    {
        private readonly IBookPurchaseRepository _purchaseRepository;

        public GetBuyHistoryQueryHandler(
            IBookPurchaseRepository purchaseRepository,
            ILogger<GetBuyHistoryQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<AppResult<PagedResult<BuyHistoryItemResponse>>> Handle(
            GetBuyHistoryQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (items, totalCount) = await _purchaseRepository.GetBuyHistoryAsync(
                    request.UserId, request.PageNumber, request.PageSize, request.SearchTerm, cancellationToken);

                return new PagedResult<BuyHistoryItemResponse>(
                    items, request.PageNumber, request.PageSize, totalCount);
            }, "Buy history retrieved successfully.");
        }
    }
}