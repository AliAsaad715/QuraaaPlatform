using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Comments.Commands.DeleteComment
{
    public record DeleteCommentCommand(Guid CommentId, Guid UserId, Guid BookId) : IRequest<AppResult>;
}
