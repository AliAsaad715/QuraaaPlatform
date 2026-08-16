using Quraaa.Application.Features.Admin.Common;
using Quraaa.Domain.Marketplace;

namespace Quraaa.Application.Features.Listings.Interfaces
{
    /// <summary>
    /// Bulk lifecycle operations a library owner performs on their own
    /// listings. Removing a listing parks it (status Removed) and is
    /// reversible; permanent deletion is only allowed from that state and only
    /// when nothing references the listing.
    /// </summary>
    public interface IListingModerationRepository
    {
        /// <summary>
        /// Loads only listings that belong to this library, so a bulk request
        /// can never reach someone else's inventory.
        /// </summary>
        Task<IReadOnlyCollection<ListingAggregate>> GetByIdsForLibraryAsync(
            Guid libraryId,
            IReadOnlyCollection<Guid> listingIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<EntityDeletionBlocker>>> GetDeletionBlockersAsync(
            IReadOnlyCollection<Guid> listingIds,
            CancellationToken cancellationToken = default);

        void Remove(IReadOnlyCollection<ListingAggregate> listings);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
