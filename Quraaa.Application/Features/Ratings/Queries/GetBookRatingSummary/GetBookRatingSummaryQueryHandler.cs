using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Ratings.Common;
using Quraaa.Application.Features.Ratings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Ratings.Queries.GetBookRatingSummary
{
    public class GetBookRatingSummaryQueryHandler
        : BaseApplicationService<GetBookRatingSummaryQueryHandler>,
          IRequestHandler<GetBookRatingSummaryQuery, AppResult<BookRatingSummaryResponse>>
    {
        private readonly IBookRatingRepository _bookRatingRepository;

        public GetBookRatingSummaryQueryHandler(
            IBookRatingRepository bookRatingRepository,
            ILogger<GetBookRatingSummaryQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookRatingRepository = bookRatingRepository;
        }

        public async Task<AppResult<BookRatingSummaryResponse>> Handle(
            GetBookRatingSummaryQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetBookRatingSummaryQuery, BookRatingSummaryResponse>(request, async () =>
            {
                if (!await _bookRatingRepository.BookExistsAsync(request.BookId, cancellationToken))
                {
                    throw new NotFoundException("Book was not found.");
                }

                return await _bookRatingRepository.GetSummaryByBookIdAsync(request.BookId, cancellationToken);
            }, "Rating summary retrieved successfully");
        }
    }
}
