using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Features.Reviews.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Reviews.Queries.GetBookReviews
{
    public class GetBookReviewsQueryHandler
        : BaseApplicationService<GetBookReviewsQueryHandler>,
          IRequestHandler<GetBookReviewsQuery, AppResult<PagedResult<BookReviewResponse>>>
    {
        private readonly IBookReviewRepository _bookReviewRepository;

        public GetBookReviewsQueryHandler(
            IBookReviewRepository bookReviewRepository,
            ILogger<GetBookReviewsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookReviewRepository = bookReviewRepository;
        }

        public async Task<AppResult<PagedResult<BookReviewResponse>>> Handle(
            GetBookReviewsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetBookReviewsQuery, PagedResult<BookReviewResponse>>(request, async () =>
            {
                if (!await _bookReviewRepository.BookExistsAsync(request.BookId, cancellationToken))
                {
                    throw new NotFoundException("Book was not found.");
                }

                var (reviews, totalCount) = await _bookReviewRepository.GetPagedByBookIdAsync(
                    request.BookId,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                return new PagedResult<BookReviewResponse>(
                    reviews,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Reviews retrieved successfully");
        }
    }
}
