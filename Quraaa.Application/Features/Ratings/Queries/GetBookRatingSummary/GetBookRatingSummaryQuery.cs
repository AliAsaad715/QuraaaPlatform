using MediatR;
using Quraaa.Application.Features.Ratings.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Ratings.Queries.GetBookRatingSummary
{
    public record GetBookRatingSummaryQuery(Guid BookId) : IRequest<AppResult<BookRatingSummaryResponse>>;
}
