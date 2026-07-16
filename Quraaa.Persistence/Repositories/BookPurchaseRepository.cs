using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Domain.Purchases;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookPurchaseRepository : IBookPurchaseRepository
    {
        private readonly ApplicationDbContext _context;

        public BookPurchaseRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<BookPurchaseAggregate> purchases, CancellationToken cancellationToken = default)
        {
            await _context.BookPurchases.AddRangeAsync(purchases, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
