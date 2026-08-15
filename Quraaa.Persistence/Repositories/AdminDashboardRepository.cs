using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class AdminDashboardRepository : IAdminDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            // Sequential, not parallel: these all share one scoped DbContext,
            // which cannot run concurrent operations.
            var totalLibrariesCount = await _context.Libraries
                .AsNoTracking()
                .CountAsync(library => !library.IsDeleted, cancellationToken);

            // Platform totals count the whole catalogue, withheld books included.
            var totalBooksCount = await _context.Books
                .IgnoreQueryFilters()
                .AsNoTracking()
                .CountAsync(book => !book.IsDeleted, cancellationToken);

            var totalActiveListingsCount = await _context.Listings
                .AsNoTracking()
                .CountAsync(
                    listing => !listing.IsDeleted && listing.Status == ListingStatus.Active,
                    cancellationToken);

            var totalUsersCount = await _context.UsersProfiles
                .AsNoTracking()
                .CountAsync(user => !user.IsDeleted, cancellationToken);

            var utcNow = DateTime.UtcNow;
            var monthStartUtc = new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var newUsersThisMonth = await _context.UsersProfiles
                .AsNoTracking()
                .CountAsync(
                    user => !user.IsDeleted && user.CreationTime >= monthStartUtc,
                    cancellationToken);

            var totalOrdersCount = await _context.Orders
                .AsNoTracking()
                .CountAsync(order => !order.IsDeleted, cancellationToken);

            var pendingOrdersCount = await _context.Orders
                .AsNoTracking()
                .CountAsync(
                    order => !order.IsDeleted && order.Status == OrderStatus.Pending,
                    cancellationToken);

            return new AdminDashboardStatsDto(
                new LibraryStatsDto(totalLibrariesCount),
                new CatalogStatsDto(totalBooksCount, totalActiveListingsCount),
                new UserStatsDto(totalUsersCount, newUsersThisMonth),
                new OrderStatsDto(totalOrdersCount, pendingOrdersCount));
        }
    }
}
