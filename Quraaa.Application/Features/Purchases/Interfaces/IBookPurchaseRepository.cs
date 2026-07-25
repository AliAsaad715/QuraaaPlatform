using Quraaa.Application.Features.Purchases.Queries.GetBuyHistory;
using Quraaa.Application.Features.Purchases.Queries.GetSellHistory;
using Quraaa.Domain.Purchases;

namespace Quraaa.Application.Features.Purchases.Interfaces
{
    public interface IBookPurchaseRepository
    {
        Task AddRangeAsync(IEnumerable<BookPurchaseAggregate> purchases, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
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
