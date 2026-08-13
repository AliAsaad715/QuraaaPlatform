using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Comments.Common;
using Quraaa.Application.Features.Comments.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Comments;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Comments.Commands.AddComment
{
    public class AddCommentCommandHandler
        : BaseApplicationService<AddCommentCommandHandler>,
          IRequestHandler<AddCommentCommand, AppResult<CommentResponse>>
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IUserRepository _userRepository;

        public AddCommentCommandHandler(
            ICommentRepository commentRepository,
            IUserRepository userRepository,
            ILogger<AddCommentCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _commentRepository = commentRepository;
            _userRepository = userRepository;
        }

        public async Task<AppResult<CommentResponse>> Handle(
            AddCommentCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<AddCommentCommand, CommentResponse>(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user is null)
                {
                    throw new NotFoundException("User was not found.");
                }

                if (!await _commentRepository.BookExistsAsync(request.BookId, cancellationToken))
                {
                    throw new NotFoundException("Book was not found.");
                }

                var comment = CommentAggregate.Create(request.UserId, request.BookId, request.Content);

                await _commentRepository.AddAsync(comment, cancellationToken);
                await _commentRepository.SaveChangesAsync(cancellationToken);

                return new CommentResponse(
                    comment.Id,
                    comment.BookId,
                    comment.UserId,
                    $"{user.FirstName} {user.LastName}",
                    comment.Content,
                    comment.CreationTime,
                    comment.LastModificationTime);
            }, "Comment added successfully");
        }
    }
}
