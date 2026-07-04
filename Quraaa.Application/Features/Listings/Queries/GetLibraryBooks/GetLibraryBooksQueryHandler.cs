using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Listings.Queries.GetLibraryBooks
{
    public class GetLibraryBooksQueryHandler
        : BaseApplicationService<GetLibraryBooksQueryHandler>,
          IRequestHandler<GetLibraryBooksQuery, AppResult<PagedResult<ListingSummaryResponse>>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public GetLibraryBooksQueryHandler(
            ILibraryRepository libraryRepository,
            ILogger<GetLibraryBooksQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<PagedResult<ListingSummaryResponse>>> Handle(
            GetLibraryBooksQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (items, totalCount) = await _libraryRepository.GetLibraryBooksAsync(
                    request.LibraryId,
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    request.SortBy,
                    request.SortDescending,
                    cancellationToken);

                // Repository already projects directly to ListingSummaryResponse
                return new PagedResult<ListingSummaryResponse>(
                    items, request.PageNumber, request.PageSize, totalCount);

            }, "Library books retrieved successfully.");
        }
    }
}