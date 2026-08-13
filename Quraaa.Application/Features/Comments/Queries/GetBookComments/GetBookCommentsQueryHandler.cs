using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Comments.Common;
using Quraaa.Application.Features.Comments.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Comments.Queries.GetBookComments
{
    public class GetBookCommentsQueryHandler
        : BaseApplicationService<GetBookCommentsQueryHandler>,
          IRequestHandler<GetBookCommentsQuery, AppResult<PagedResult<CommentResponse>>>
    {
        private readonly ICommentRepository _commentRepository;

        public GetBookCommentsQueryHandler(
            ICommentRepository commentRepository,
            ILogger<GetBookCommentsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _commentRepository = commentRepository;
        }

        public async Task<AppResult<PagedResult<CommentResponse>>> Handle(
            GetBookCommentsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetBookCommentsQuery, PagedResult<CommentResponse>>(request, async () =>
            {
                if (!await _commentRepository.BookExistsAsync(request.BookId, cancellationToken))
                {
                    throw new NotFoundException("Book was not found.");
                }

                var (comments, totalCount) = await _commentRepository.GetPagedByBookIdAsync(
                    request.BookId,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new PagedResult<CommentResponse>(
                    comments,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Comments retrieved successfully");
        }
    }
}
