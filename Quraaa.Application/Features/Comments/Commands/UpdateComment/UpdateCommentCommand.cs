using MediatR;
using Quraaa.Application.Features.Comments.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Comments.Commands.UpdateComment
{
    public record UpdateCommentCommand(Guid CommentId, Guid UserId, Guid BookId, string Content) : IRequest<AppResult<CommentResponse>>;
}
