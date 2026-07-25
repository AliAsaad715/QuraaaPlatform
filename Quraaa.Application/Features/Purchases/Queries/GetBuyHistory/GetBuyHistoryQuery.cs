using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Purchases.Queries.GetBuyHistory
{
    public record GetBuyHistoryQuery(
        Guid UserId,
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null
    ) : IRequest<AppResult<PagedResult<BuyHistoryItemResponse>>>;
}