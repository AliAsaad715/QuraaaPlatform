using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Payouts.Queries.GetLibraryPayouts
{
    public sealed record GetLibraryPayoutsQuery(
        Guid RequestingUserId,
        int PageNumber = 1,
        int PageSize = 10)
        : IRequest<AppResult<PagedResult<SellerPayoutResponse>>>;
}
