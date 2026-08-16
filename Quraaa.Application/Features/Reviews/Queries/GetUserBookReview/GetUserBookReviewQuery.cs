using MediatR;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Reviews.Queries.GetUserBookReview
{
    public record GetUserBookReviewQuery(Guid UserId, Guid BookId) : IRequest<AppResult<BookReviewResponse>>;
}
