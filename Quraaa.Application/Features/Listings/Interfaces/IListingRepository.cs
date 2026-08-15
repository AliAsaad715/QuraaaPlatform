using Quraaa.Application.Features.Listings.Queries.GetListingById;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
// Aliased: GetListingDetails also declares a "ListingDetailsResponse", distinct from the
// GetListingById one already imported above.
using MobileListingDetailsResponse = Quraaa.Application.Features.Listings.Queries.GetListingDetails.ListingDetailsResponse;

namespace Quraaa.Application.Features.Listings.Interfaces
{
    public interface IListingRepository
    {
        Task<ListingAggregate?> GetByIdAsync(Guid listingId,
            CancellationToken cancellationToken = default);

        Task<ListingAggregate?> GetByIdForInventoryAsync(
            Guid listingId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<Guid, ListingFormat>> GetActiveFormatsByIdsAsync(
            IReadOnlyCollection<Guid> listingIds,
            CancellationToken cancellationToken = default);

        /// <summary>Joined projection used by the Get-by-ID query.</summary>
        Task<ListingDetailsResponse?> GetByIdWithDetailsAsync(Guid listingId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Joined projection used by the public listing-details query (mobile app book details
        /// screen): book, author, seller (library name or user full name), rating summary, and
        /// a short recent-reviews preview.
        /// </summary>
        Task<MobileListingDetailsResponse?> GetListingDetailsAsync(Guid listingId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsByLibraryAndBookAsync(Guid libraryId, Guid bookId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsByUserAndBookAsync(Guid userId, Guid bookId,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<ListingSummaryResponse> Items, int TotalCount)> GetUserBooksForSaleAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            CancellationToken cancellationToken = default);

        Task AddAsync(ListingAggregate listing,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the subset of <paramref name="relativePaths"/> that are still referenced by
        /// some listing's current digital asset column, regardless of the listing's status —
        /// a removed or out-of-stock listing still owns its file.
        /// </summary>
        Task<HashSet<string>> FilterReferencedDigitalAssetPathsAsync(
            IReadOnlyCollection<string> relativePaths,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
