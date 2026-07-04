using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Queries.GetListingById
{
    public class GetListingByIdQueryHandler
        : BaseApplicationService<GetListingByIdQueryHandler>,
          IRequestHandler<GetListingByIdQuery, AppResult<ListingDetailsResponse>>
    {
        private readonly IListingRepository _listingRepository;

        public GetListingByIdQueryHandler(
            IListingRepository listingRepository,
            ILogger<GetListingByIdQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _listingRepository = listingRepository;
        }

        public async Task<AppResult<ListingDetailsResponse>> Handle(
            GetListingByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var response = await _listingRepository
                    .GetByIdWithDetailsAsync(request.ListingId, cancellationToken);

                if (response is null)
                    throw new NotFoundException("Listing not found.");

                return response;

            }, "Listing retrieved successfully.");
        }
    }
}