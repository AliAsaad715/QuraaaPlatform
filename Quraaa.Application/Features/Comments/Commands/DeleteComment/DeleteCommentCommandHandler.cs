using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Comments.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Comments.Commands.DeleteComment
{
    public class DeleteCommentCommandHandler
        : BaseApplicationService<DeleteCommentCommandHandler>,
          IRequestHandler<DeleteCommentCommand, AppResult>
    {
        private readonly ICommentRepository _commentRepository;

        public DeleteCommentCommandHandler(
            ICommentRepository commentRepository,
            ILogger<DeleteCommentCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _commentRepository = commentRepository;
        }

        public async Task<AppResult> Handle(
            DeleteCommentCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var comment = await _commentRepository.GetByIdAsync(request.CommentId, cancellationToken);
                if (comment is null || comment.BookId != request.BookId)
                {
                    throw new NotFoundException("Comment was not found.");
                }

                if (comment.UserId != request.UserId)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to this comment");
                }

                comment.Delete(request.UserId);

                await _commentRepository.SaveChangesAsync(cancellationToken);
            }, "Comment deleted successfully");
        }
    }
}
