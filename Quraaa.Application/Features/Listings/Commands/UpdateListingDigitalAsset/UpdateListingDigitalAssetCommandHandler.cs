using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Commands.UpdateListingDigitalAsset
{
    public class UpdateListingDigitalAssetCommandHandler
        : BaseApplicationService<UpdateListingDigitalAssetCommandHandler>,
          IRequestHandler<UpdateListingDigitalAssetCommand, AppResult>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IListingRepository _listingRepository;
        private readonly ILibraryBookStorageService _libraryBookStorageService;

        public UpdateListingDigitalAssetCommandHandler(
            ILibraryRepository libraryRepository,
            IListingRepository listingRepository,
            ILibraryBookStorageService libraryBookStorageService,
            ILogger<UpdateListingDigitalAssetCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _listingRepository = listingRepository;
            _libraryBookStorageService = libraryBookStorageService;
        }

        public async Task<AppResult> Handle(
            UpdateListingDigitalAssetCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                // ── Verify the listing exists ─────────────────────────────────
                var listing = await _listingRepository
                    .GetByIdAsync(request.ListingId, cancellationToken);

                if (listing is null)
                    throw new NotFoundException("Listing not found");

                // ── Verify the caller owns this listing's library ─────────────
                var library = await _libraryRepository
                    .GetApprovedByUserIdAsync(request.RequestingUserId, cancellationToken);

                if (library is null || listing.LibraryId != library.Id)
                    throw new UnauthorizedAccessException("Unauthorized access to this listing");

                if (listing.Format != ListingFormat.Digital)
                    throw new DomainException("Only digital listings have a digital asset.");

                // ── Upload first, then persist — matches AddDigitalBook's order ─
                var newUrl = await _libraryBookStorageService.SaveAsync(
                    request.DigitalAsset, cancellationToken);

                listing.UpdateCustomDigitalAsset(newUrl, request.RequestingUserId);

                await _listingRepository.SaveChangesAsync(cancellationToken);

            }, "Listing digital asset updated successfully.");
        }
    }
}
