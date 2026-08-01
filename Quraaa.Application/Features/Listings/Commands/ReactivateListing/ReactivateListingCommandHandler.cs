using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Commands.ReactivateListing
{
    public class ReactivateListingCommandHandler
        : BaseApplicationService<ReactivateListingCommandHandler>,
          IRequestHandler<ReactivateListingCommand, AppResult>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IListingRepository _listingRepository;

        public ReactivateListingCommandHandler(
            ILibraryRepository libraryRepository,
            IListingRepository listingRepository,
            ILogger<ReactivateListingCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _listingRepository = listingRepository;
        }

        public async Task<AppResult> Handle(
            ReactivateListingCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var listing = await _listingRepository.GetByIdForInventoryAsync(
                    request.ListingId,
                    cancellationToken);

                if (listing is null)
                {
                    throw new NotFoundException("Listing not found");
                }

                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.RequestingUserId,
                    cancellationToken);

                if (library is null || listing.LibraryId != library.Id)
                {
                    throw new UnauthorizedAccessException("Unauthorized access to this listing");
                }

                if (listing.Status != ListingStatus.Removed)
                {
                    throw new ConflictException("Only removed listings can be reactivated.");
                }

                listing.Reactivate(request.RequestingUserId);
                await _listingRepository.SaveChangesAsync(cancellationToken);

            }, "Listing reactivated successfully.");
        }
    }
}