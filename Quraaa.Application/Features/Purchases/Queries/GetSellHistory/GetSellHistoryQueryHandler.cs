using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Purchases.Queries.GetSellHistory
{
    public class GetSellHistoryQueryHandler
        : BaseApplicationService<GetSellHistoryQueryHandler>,
          IRequestHandler<GetSellHistoryQuery, AppResult<PagedResult<SellHistoryItemResponse>>>
    {
        private readonly IBookPurchaseRepository _purchaseRepository;

        public GetSellHistoryQueryHandler(
            IBookPurchaseRepository purchaseRepository,
            ILogger<GetSellHistoryQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _purchaseRepository = purchaseRepository;
        }

        public async Task<AppResult<PagedResult<SellHistoryItemResponse>>> Handle(
            GetSellHistoryQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (items, totalCount) = await _purchaseRepository.GetSellHistoryAsync(
                    request.UserId, request.PageNumber, request.PageSize, request.SearchTerm, cancellationToken);

                return new PagedResult<SellHistoryItemResponse>(
                    items, request.PageNumber, request.PageSize, totalCount);
            }, "Sell history retrieved successfully.");
        }
    }
}