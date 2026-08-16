using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Domain.Marketplace;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories;

public class ListingModerationRepository : IListingModerationRepository
{
    private readonly ApplicationDbContext _context;

    public ListingModerationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<ListingAggregate>> GetByIdsForLibraryAsync(
        Guid libraryId,
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken = default) =>
        await _context.Listings
            .IgnoreQueryFilters()
            .Where(listing => listing.LibraryId == libraryId
                && listingIds.Contains(listing.Id)
                && !listing.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<EntityDeletionBlocker>>> GetDeletionBlockersAsync(
        IReadOnlyCollection<Guid> listingIds,
        CancellationToken cancellationToken = default)
    {
        var ids = listingIds.Distinct().ToArray();
        var blockers = ids.ToDictionary(id => id, _ => new List<EntityDeletionBlocker>());

        // A listing that was ever bought, ordered, or is sitting in a cart
        // carries history that must not be orphaned.
        var sources = new (string Reference, IQueryable<Guid> Owners)[]
        {
            ("Purchases", _context.BookPurchases.IgnoreQueryFilters()
                .Where(purchase => ids.Contains(purchase.ListingId) && !purchase.IsDeleted)
                .Select(purchase => purchase.ListingId)),
            ("Order items", _context.Orders.IgnoreQueryFilters()
                .SelectMany(order => order.Items)
                .Where(item => ids.Contains(item.ListingId))
                .Select(item => item.ListingId)),
            ("Carts", _context.Carts.IgnoreQueryFilters()
                .SelectMany(cart => cart.Items)
                .Where(item => ids.Contains(item.ListingId))
                .Select(item => item.ListingId)),
        };

        foreach (var (reference, owners) in sources)
        {
            var counts = await owners
                .GroupBy(ownerId => ownerId)
                .Select(group => new { OwnerId = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);

            foreach (var row in counts)
            {
                if (blockers.TryGetValue(row.OwnerId, out var list))
                {
                    list.Add(new EntityDeletionBlocker(reference, row.Count));
                }
            }
        }

        return blockers.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyCollection<EntityDeletionBlocker>)pair.Value);
    }

    public void Remove(IReadOnlyCollection<ListingAggregate> listings) =>
        _context.Listings.RemoveRange(listings);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
