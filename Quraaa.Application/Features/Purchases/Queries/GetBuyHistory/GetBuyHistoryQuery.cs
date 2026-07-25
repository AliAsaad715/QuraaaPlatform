using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Purchases.Queries.GetBuyHistory
{
    public record GetBuyHistoryQuery(
        Guid UserId,
        int PageNumber,
        int PageSize,
        string? SearchTerm = null
    ) : IRequest<AppResult<PagedResult<BuyHistoryItemResponse>>>;
}