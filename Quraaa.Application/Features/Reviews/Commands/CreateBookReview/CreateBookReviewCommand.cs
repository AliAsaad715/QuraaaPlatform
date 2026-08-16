using MediatR;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Reviews.Commands.CreateBookReview
{
    public record CreateBookReviewCommand(Guid UserId, Guid BookId, int Score, string Content)
        : IRequest<AppResult<BookReviewResponse>>;
}
