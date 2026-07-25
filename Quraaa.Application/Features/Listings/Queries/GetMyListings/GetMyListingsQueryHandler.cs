using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Listings.Queries.GetMyListings
{
    public class GetMyListingsQueryHandler
        : BaseApplicationService<GetMyListingsQueryHandler>,
          IRequestHandler<GetMyListingsQuery, AppResult<PagedResult<ListingSummaryResponse>>>
    {
        private readonly IListingRepository _listingRepository;

        public GetMyListingsQueryHandler(
            IListingRepository listingRepository,
            ILogger<GetMyListingsQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _listingRepository = listingRepository;
        }

        public async Task<AppResult<PagedResult<ListingSummaryResponse>>> Handle(
            GetMyListingsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (items, totalCount) = await _listingRepository.GetUserBooksForSaleAsync(
                    request.UserId,
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    request.SortBy,
                    request.SortDescending,
                    cancellationToken);

                return new PagedResult<ListingSummaryResponse>(items, request.PageNumber, request.PageSize, totalCount);

            }, "User listings retrieved successfully.");
        }
    }
}