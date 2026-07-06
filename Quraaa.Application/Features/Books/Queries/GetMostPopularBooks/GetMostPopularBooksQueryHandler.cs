using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Books.Queries.GetMostPopularBooks
{
    public class GetMostPopularBooksQueryHandler
        : BaseApplicationService<GetMostPopularBooksQueryHandler>,
          IRequestHandler<GetMostPopularBooksQuery, AppResult<PagedResult<PopularBookResponse>>>
    {
        private readonly IBookPopularityRepository _bookPopularityRepository;

        public GetMostPopularBooksQueryHandler(
            IBookPopularityRepository bookPopularityRepository,
            ILogger<GetMostPopularBooksQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookPopularityRepository = bookPopularityRepository;
        }

        public async Task<AppResult<PagedResult<PopularBookResponse>>> Handle(
            GetMostPopularBooksQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetMostPopularBooksQuery, PagedResult<PopularBookResponse>>(request, async () =>
            {
                var (books, totalCount) = await _bookPopularityRepository.GetMostPopularAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    request.SortBy,
                    request.IncludeUnranked,
                    cancellationToken);

                return new PagedResult<PopularBookResponse>(
                    books,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Most popular books retrieved successfully");
        }
    }
}
