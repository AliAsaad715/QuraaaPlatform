using Quraaa.Application.Features.Listings.Queries.GetListingById;
using Quraaa.Domain.Marketplace;

namespace Quraaa.Application.Features.Listings.Interfaces
{
    public interface IListingRepository
    {
        Task<ListingAggregate?> GetByIdAsync(Guid listingId,
            CancellationToken cancellationToken = default);

        /// <summary>Joined projection used by the Get-by-ID query.</summary>
        Task<ListingDetailsResponse?> GetByIdWithDetailsAsync(Guid listingId,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsByLibraryAndBookAsync(Guid libraryId, Guid bookId,
            CancellationToken cancellationToken = default);

        Task AddAsync(ListingAggregate listing,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}