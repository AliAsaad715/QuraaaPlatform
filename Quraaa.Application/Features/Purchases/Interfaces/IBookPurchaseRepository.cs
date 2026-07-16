using Quraaa.Domain.Purchases;

namespace Quraaa.Application.Features.Purchases.Interfaces
{
    public interface IBookPurchaseRepository
    {
        Task AddRangeAsync(IEnumerable<BookPurchaseAggregate> purchases, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
