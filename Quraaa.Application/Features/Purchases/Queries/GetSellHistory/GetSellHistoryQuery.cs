using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Purchases.Queries.GetSellHistory
{
    public record GetSellHistoryQuery(
        Guid UserId,
        int PageNumber,
        int PageSize,
        string? SearchTerm = null
    ) : IRequest<AppResult<PagedResult<SellHistoryItemResponse>>>;
}