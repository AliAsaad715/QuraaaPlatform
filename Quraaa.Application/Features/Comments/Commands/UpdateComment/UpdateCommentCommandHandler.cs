using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Comments.Common;
using Quraaa.Application.Features.Comments.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Comments.Commands.UpdateComment
{
    public class UpdateCommentCommandHandler
        : BaseApplicationService<UpdateCommentCommandHandler>,
          IRequestHandler<UpdateCommentCommand, AppResult<CommentResponse>>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IUserRepository _userRepository;

        public UpdateCommentCommandHandler(
            ICommentRepository commentRepository,
            IUserRepository userRepository,
            ILogger<UpdateCommentCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _commentRepository = commentRepository;
            _userRepository = userRepository;
        }

        public async Task<AppResult<CommentResponse>> Handle(
            UpdateCommentCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<UpdateCommentCommand, CommentResponse>(request, async () =>
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

                comment.UpdateContent(request.Content, request.UserId);

                await _commentRepository.SaveChangesAsync(cancellationToken);

                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                return new CommentResponse(
                    comment.Id,
                    comment.BookId,
                    comment.UserId,
                    $"{user.FirstName} {user.LastName}",
                    comment.Content,
                    comment.CreationTime,
                    comment.LastModificationTime);
            }, "Comment updated successfully");
        }
    }
}
