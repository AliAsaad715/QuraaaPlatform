using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Queries.GetListingDetails
{
    public class GetListingDetailsQueryHandler
        : BaseApplicationService<GetListingDetailsQueryHandler>,
          IRequestHandler<GetListingDetailsQuery, AppResult<ListingDetailsResponse>>
    {
        private readonly IListingRepository _listingRepository;

        public GetListingDetailsQueryHandler(
            IListingRepository listingRepository,
            ILogger<GetListingDetailsQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _listingRepository = listingRepository;
        }

        public async Task<AppResult<ListingDetailsResponse>> Handle(
            GetListingDetailsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var response = await _listingRepository
                    .GetListingDetailsAsync(request.Id, cancellationToken);

                if (response is null)
                    throw new NotFoundException("Listing not found.");

                return response;

            }, "Listing details retrieved successfully.");
        }
    }
}
