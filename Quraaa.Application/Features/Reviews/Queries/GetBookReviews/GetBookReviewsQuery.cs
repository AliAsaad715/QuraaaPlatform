using MediatR;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Reviews.Queries.GetBookReviews
{
    public record GetBookReviewsQuery(
        Guid BookId,
        int PageNumber = 1,
        int PageSize = 20) : IRequest<AppResult<PagedResult<BookReviewResponse>>>;
}
