using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Payouts.Queries.GetLibraryProfitShare
{
    /// <summary>
    /// Administrator query: the current profit-share setting of one library.
    /// </summary>
    public record GetLibraryProfitShareQuery(Guid LibraryId)
        : IRequest<AppResult<LibraryProfitShareResponse>>;
}
