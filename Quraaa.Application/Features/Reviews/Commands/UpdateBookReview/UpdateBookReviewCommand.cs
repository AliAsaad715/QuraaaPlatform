using MediatR;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Reviews.Commands.UpdateBookReview
{
    public record UpdateBookReviewCommand(Guid UserId, Guid BookId, int Score, string Content)
        : IRequest<AppResult<BookReviewResponse>>;
}
