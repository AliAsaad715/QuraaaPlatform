using MediatR;
using Quraaa.Application.Features.Comments.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Comments.Commands.AddComment
{
    public record AddCommentCommand(Guid UserId, Guid BookId, string Content) : IRequest<AppResult<CommentResponse>>;
}
