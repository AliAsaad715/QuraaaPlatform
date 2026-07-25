using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Catalog.Common;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Features.Purchases.Queries.GetBuyHistory;
using Quraaa.Application.Features.Purchases.Queries.GetSellHistory;
using Quraaa.Domain.Marketplace.Enums;
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

        public async Task<(IReadOnlyCollection<BuyHistoryItemResponse> Items, int TotalCount)> GetBuyHistoryAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.BookPurchases
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Join(
                    _context.Books.AsNoTracking(),
                    p => p.BookId,
                    b => b.Id,
                    (p, b) => new { Purchase = p, Book = b })
                .GroupJoin(
                    _context.Categories.AsNoTracking(),
                    x => x.Book.CategoryId,
                    c => c.Id,
                    (x, categories) => new { x.Purchase, x.Book, Categories = categories })
                .SelectMany(
                    x => x.Categories.DefaultIfEmpty(),
                    (x, c) => new { x.Purchase, x.Book, Category = c });

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(x =>
                    EF.Functions.ILike(x.Book.Title, $"%{normalized}%") ||
                    EF.Functions.ILike(x.Book.Author, $"%{normalized}%"));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.Purchase.CreationTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new BuyHistoryItemResponse(
                    x.Purchase.Id,
                    new BookDetails(
                        x.Book.Id,
                        x.Book.Title,
                        x.Book.Author,
                        x.Book.Description,
                        x.Book.CoverImageUrl,
                        x.Book.Language,
                        x.Book.Isbn,
                        x.Category == null ? null : new CategoryResponse(x.Category.Id, x.Category.NameEn, x.Category.NameAr)),
                    x.Purchase.Quantity,
                    x.Purchase.UnitPrice,
                    x.Purchase.Quantity * x.Purchase.UnitPrice,
                    x.Purchase.CreationTime))
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<(IReadOnlyCollection<SellHistoryItemResponse> Items, int TotalCount)> GetSellHistoryAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.BookPurchases
                .AsNoTracking()
                .Join(
                    _context.Listings.AsNoTracking(),
                    p => p.ListingId,
                    l => l.Id,
                    (p, l) => new { Purchase = p, Listing = l })
                .Where(x => x.Listing.SellerType == SellerType.User && x.Listing.UserId == userId)
                .Join(
                    _context.Books.AsNoTracking(),
                    x => x.Purchase.BookId,
                    b => b.Id,
                    (x, b) => new { x.Purchase, Book = b })
                .GroupJoin(
                    _context.Categories.AsNoTracking(),
                    x => x.Book.CategoryId,
                    c => c.Id,
                    (x, categories) => new { x.Purchase, x.Book, Categories = categories })
                .SelectMany(
                    x => x.Categories.DefaultIfEmpty(),
                    (x, c) => new { x.Purchase, x.Book, Category = c });

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(x =>
                    EF.Functions.ILike(x.Book.Title, $"%{normalized}%") ||
                    EF.Functions.ILike(x.Book.Author, $"%{normalized}%"));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.Purchase.CreationTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SellHistoryItemResponse(
                    x.Purchase.Id,
                    new BookDetails(
                        x.Book.Id,
                        x.Book.Title,
                        x.Book.Author,
                        x.Book.Description,
                        x.Book.CoverImageUrl,
                        x.Book.Language,
                        x.Book.Isbn,
                        x.Category == null ? null : new CategoryResponse(x.Category.Id, x.Category.NameEn, x.Category.NameAr)),
                    x.Purchase.Quantity,
                    x.Purchase.UnitPrice,
                    x.Purchase.Quantity * x.Purchase.UnitPrice,
                    x.Purchase.UserId, // BuyerUserId
                    x.Purchase.CreationTime))
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
