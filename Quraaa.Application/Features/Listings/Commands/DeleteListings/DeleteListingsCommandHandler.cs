using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Commands.DeleteListings
{
    public class DeleteListingsCommandHandler
        : BaseApplicationService<DeleteListingsCommandHandler>,
          IRequestHandler<DeleteListingsCommand, AppResult<BulkModerationResult>>
    {
        private readonly IListingModerationRepository _listingModerationRepository;
        private readonly ILibraryRepository _libraryRepository;

        public DeleteListingsCommandHandler(
            IListingModerationRepository listingModerationRepository,
            ILibraryRepository libraryRepository,
            ILogger<DeleteListingsCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _listingModerationRepository = listingModerationRepository;
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<BulkModerationResult>> Handle(
            DeleteListingsCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<DeleteListingsCommand, BulkModerationResult>(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.RequestingUserId,
                    cancellationToken)
                    ?? throw new NotFoundException("Library not found");

                var ids = request.Ids.Distinct().ToArray();

                var listings = await _listingModerationRepository.GetByIdsForLibraryAsync(
                    library.Id,
                    ids,
                    cancellationToken);

                var byId = listings.ToDictionary(listing => listing.Id);

                var blockersById = await _listingModerationRepository.GetDeletionBlockersAsync(
                    ids,
                    cancellationToken);

                var outcomes = new List<BulkModerationOutcome>(ids.Length);
                var removable = new List<ListingAggregate>();

                foreach (var id in ids)
                {
                    if (!byId.TryGetValue(id, out var listing))
                    {
                        outcomes.Add(new BulkModerationOutcome(
                            id, false, AdminModerationErrorCodes.NotFound));
                        continue;
                    }

                    if (listing.Status != ListingStatus.Removed)
                    {
                        outcomes.Add(new BulkModerationOutcome(
                            id,
                            false,
                            "Remove this listing from sale before deleting it permanently."));
                        continue;
                    }

                    if (blockersById.TryGetValue(id, out var blockers) && blockers.Count > 0)
                    {
                        outcomes.Add(new BulkModerationOutcome(
                            id, false, AdminModerationErrorCodes.StillReferenced, blockers));
                        continue;
                    }

                    removable.Add(listing);
                    outcomes.Add(new BulkModerationOutcome(id, true));
                }

                if (removable.Count > 0)
                {
                    _listingModerationRepository.Remove(removable);
                    await _listingModerationRepository.SaveChangesAsync(cancellationToken);
                }

                Logger.LogWarning(
                    "Library {LibraryId} permanently deleted {DeletedCount} of {RequestedCount} listings.",
                    library.Id,
                    removable.Count,
                    ids.Length);

                return BulkModerationResult.From(outcomes);
            }, "Listings deleted successfully");
        }
    }
}
