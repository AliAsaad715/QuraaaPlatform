using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Reviews.Commands.DeleteBookReview
{
    public record DeleteBookReviewCommand(Guid UserId, Guid BookId) : IRequest<AppResult>;
}
