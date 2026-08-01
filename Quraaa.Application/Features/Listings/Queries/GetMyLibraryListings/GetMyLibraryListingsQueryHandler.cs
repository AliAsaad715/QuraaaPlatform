using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Queries.GetMyLibraryListings
{
    public class GetMyLibraryListingsQueryHandler
        : BaseApplicationService<GetMyLibraryListingsQueryHandler>,
          IRequestHandler<GetMyLibraryListingsQuery, AppResult<PagedResult<ListingSummaryResponse>>>
    {
        private readonly ILibraryRepository _libraryRepository;

        public GetMyLibraryListingsQueryHandler(
            ILibraryRepository libraryRepository,
            ILogger<GetMyLibraryListingsQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<PagedResult<ListingSummaryResponse>>> Handle(
            GetMyLibraryListingsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found");
                }

                var (items, totalCount) = await _libraryRepository.GetLibraryBooksAsync(
                    library.Id,
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    request.SortBy,
                    request.SortDescending,
                    request.Status,
                    cancellationToken);

                return new PagedResult<ListingSummaryResponse>(items, request.PageNumber, request.PageSize, totalCount);

            }, "Library listings retrieved successfully.");
        }
    }
}