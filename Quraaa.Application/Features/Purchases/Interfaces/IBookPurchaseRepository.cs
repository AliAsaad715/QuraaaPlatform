using Quraaa.Application.Features.Purchases.Common;
using Quraaa.Application.Features.Purchases.Queries.GetBuyHistory;
using Quraaa.Application.Features.Purchases.Queries.GetSellHistory;
using Quraaa.Domain.Purchases;

namespace Quraaa.Application.Features.Purchases.Interfaces
{
    public interface IBookPurchaseRepository
    {
        Task AddRangeAsync(IEnumerable<BookPurchaseAggregate> purchases, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> HasUserPurchasedListingAsync(Guid userId, Guid listingId, CancellationToken cancellationToken = default);

        /// <summary>Owner and digital-asset snapshot for a single purchase, used to authorize downloads.</summary>
        Task<PurchaseDigitalAssetInfo?> GetDigitalAssetInfoAsync(
            Guid purchaseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Owner and book snapshot for a single purchase, used to authorize AI-assistant
        /// requests against the book the caller actually bought (see PurchaseBookContext).
        /// </summary>
        Task<PurchaseBookContext?> GetPurchaseBookContextAsync(
            Guid purchaseId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the subset of <paramref name="relativePaths"/> that are still referenced by
        /// some purchase's immutable <c>PurchasedDigitalAssetUrl</c> snapshot. These files must
        /// never be deleted, regardless of the owning listing's current state.
        /// </summary>
        Task<HashSet<string>> FilterReferencedDigitalAssetPathsAsync(
            IReadOnlyCollection<string> relativePaths,
            CancellationToken cancellationToken = default);
        Task<(IReadOnlyCollection<BuyHistoryItemResponse> Items, int TotalCount)> GetBuyHistoryAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<SellHistoryItemResponse> Items, int TotalCount)> GetSellHistoryAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CancellationToken cancellationToken = default);
    }
}
