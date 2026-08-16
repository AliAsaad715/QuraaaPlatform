using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Listings.Commands.SetListingsActivation
{
    public class SetListingsActivationCommandHandler
        : BaseApplicationService<SetListingsActivationCommandHandler>,
          IRequestHandler<SetListingsActivationCommand, AppResult<BulkModerationResult>>
    {
        private readonly IListingModerationRepository _listingModerationRepository;
        private readonly ILibraryRepository _libraryRepository;

        public SetListingsActivationCommandHandler(
            IListingModerationRepository listingModerationRepository,
            ILibraryRepository libraryRepository,
            ILogger<SetListingsActivationCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _listingModerationRepository = listingModerationRepository;
            _libraryRepository = libraryRepository;
        }

        public async Task<AppResult<BulkModerationResult>> Handle(
            SetListingsActivationCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SetListingsActivationCommand, BulkModerationResult>(request, async () =>
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
                var outcomes = new List<BulkModerationOutcome>(ids.Length);

                foreach (var id in ids)
                {
                    // A listing that is not this library's simply reads as "not
                    // found": the owner learns nothing about other inventories.
                    if (!byId.TryGetValue(id, out var listing))
                    {
                        outcomes.Add(new BulkModerationOutcome(
                            id, false, AdminModerationErrorCodes.NotFound));
                        continue;
                    }

                    if (request.Deactivate)
                    {
                        listing.Remove(request.RequestingUserId);
                        outcomes.Add(new BulkModerationOutcome(id, true));
                        continue;
                    }

                    if (listing.Status != ListingStatus.Removed)
                    {
                        outcomes.Add(new BulkModerationOutcome(
                            id, false, "Only removed listings can be restored."));
                        continue;
                    }

                    listing.Reactivate(request.RequestingUserId);
                    outcomes.Add(new BulkModerationOutcome(id, true));
                }

                await _listingModerationRepository.SaveChangesAsync(cancellationToken);

                Logger.LogInformation(
                    "Library {LibraryId} set activation ({Deactivate}) on {SucceededCount} of {RequestedCount} listings.",
                    library.Id,
                    request.Deactivate,
                    outcomes.Count(outcome => outcome.Succeeded),
                    ids.Length);

                return BulkModerationResult.From(outcomes);
            }, "Listings updated successfully");
        }
    }
}
