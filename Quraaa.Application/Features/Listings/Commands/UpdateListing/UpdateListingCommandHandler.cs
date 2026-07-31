using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Commands.UpdateListing
{
    public class UpdateListingCommandHandler
        : BaseApplicationService<UpdateListingCommandHandler>,
          IRequestHandler<UpdateListingCommand, AppResult>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IListingRepository _listingRepository;

        public UpdateListingCommandHandler(
            ILibraryRepository libraryRepository,
            IListingRepository listingRepository,
            ILogger<UpdateListingCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _listingRepository = listingRepository;
        }

        public async Task<AppResult> Handle(
            UpdateListingCommand request,
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

                // ── Apply updates (only what was supplied) ────────────────────
                var actorId = request.RequestingUserId;

                if (request.Price.HasValue)
                    listing.UpdatePrice(request.Price.Value, actorId);

                if (request.Stock.HasValue)
                    listing.UpdateStock(request.Stock.Value, actorId);

                if (request.Condition.HasValue)
                    listing.UpdateCondition(request.Condition.Value, actorId);

                await _listingRepository.SaveChangesAsync(cancellationToken);

            }, "Listing updated successfully.");
        }
    }
}