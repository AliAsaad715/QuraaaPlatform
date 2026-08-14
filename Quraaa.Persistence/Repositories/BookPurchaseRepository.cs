using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Catalog.Common;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Purchases.Common;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Features.Purchases.Queries.GetBuyHistory;
using Quraaa.Application.Features.Purchases.Queries.GetSellHistory;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Purchases;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookPurchaseRepository : IBookPurchaseRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public BookPurchaseRepository(ApplicationDbContext context, IImageUrlFormatter imageUrlFormatter)
        {
            _context = context;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task AddRangeAsync(IEnumerable<BookPurchaseAggregate> purchases, CancellationToken cancellationToken = default)
        {
            await _context.BookPurchases.AddRangeAsync(purchases, cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        public Task<bool> HasUserPurchasedListingAsync(
            Guid userId,
            Guid listingId,
            CancellationToken cancellationToken = default) =>
            _context.BookPurchases
                .AsNoTracking()
                .AnyAsync(p => p.UserId == userId && p.ListingId == listingId, cancellationToken);

        public Task<bool> HasUserPurchasedBookAsync(
            Guid userId,
            Guid bookId,
            CancellationToken cancellationToken = default) =>
            _context.BookPurchases
                .AsNoTracking()
                .AnyAsync(p => p.UserId == userId && p.BookId == bookId, cancellationToken);

        public Task<PurchaseDigitalAssetInfo?> GetDigitalAssetInfoAsync(
            Guid purchaseId,
            CancellationToken cancellationToken = default) =>
            _context.BookPurchases
                .AsNoTracking()
                .Where(p => p.Id == purchaseId)
                .Join(
                    _context.Books.AsNoTracking(),
                    p => p.BookId,
                    b => b.Id,
                    (p, b) => new PurchaseDigitalAssetInfo(p.UserId, p.PurchasedDigitalAssetUrl, b.Title))
                .FirstOrDefaultAsync(cancellationToken);

        public Task<PurchaseBookContext?> GetPurchaseBookContextAsync(
            Guid purchaseId,
            CancellationToken cancellationToken = default) =>
            _context.BookPurchases
                .AsNoTracking()
                .Where(p => p.Id == purchaseId)
                .Join(
                    _context.Books.AsNoTracking(),
                    p => p.BookId,
                    b => b.Id,
                    (p, b) => new PurchaseBookContext(
                        p.UserId, b.Id, b.Title, b.Author, b.Description, b.CanonicalPdfUrl, b.CanonicalWordDocUrl))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<HashSet<string>> FilterReferencedDigitalAssetPathsAsync(
            IReadOnlyCollection<string> relativePaths,
            CancellationToken cancellationToken = default)
        {
            if (relativePaths.Count == 0)
                return [];

            var referenced = await _context.BookPurchases
                .AsNoTracking()
                .Where(p => p.PurchasedDigitalAssetUrl != null && relativePaths.Contains(p.PurchasedDigitalAssetUrl))
                .Select(p => p.PurchasedDigitalAssetUrl!)
                .ToListAsync(cancellationToken);

            return referenced.ToHashSet(StringComparer.Ordinal);
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

            // Materialize first, then project to the response DTO in memory —
            // IImageUrlFormatter.Format can't be translated into SQL.
            var rows = await query
                .OrderByDescending(x => x.Purchase.CreationTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = rows
                .Select(x => new BuyHistoryItemResponse(
                    x.Purchase.Id,
                    new BookDetails(
                        x.Book.Id,
                        x.Book.Title,
                        x.Book.Author,
                        x.Book.Description,
                        _imageUrlFormatter.Format(x.Book.CoverImageUrl),
                        x.Book.Language,
                        x.Book.Isbn,
                        x.Category == null ? null : new CategoryResponse(x.Category.Id, x.Category.NameEn, x.Category.NameAr)),
                    x.Purchase.Quantity,
                    x.Purchase.UnitPrice,
                    x.Purchase.Quantity * x.Purchase.UnitPrice,
                    x.Purchase.CreationTime,
                    x.Purchase.PurchasedDigitalAssetUrl))
                .ToList();

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

            // Materialize first, then project to the response DTO in memory —
            // IImageUrlFormatter.Format can't be translated into SQL.
            var rows = await query
                .OrderByDescending(x => x.Purchase.CreationTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = rows
                .Select(x => new SellHistoryItemResponse(
                    x.Purchase.Id,
                    new BookDetails(
                        x.Book.Id,
                        x.Book.Title,
                        x.Book.Author,
                        x.Book.Description,
                        _imageUrlFormatter.Format(x.Book.CoverImageUrl),
                        x.Book.Language,
                        x.Book.Isbn,
                        x.Category == null ? null : new CategoryResponse(x.Category.Id, x.Category.NameEn, x.Category.NameAr)),
                    x.Purchase.Quantity,
                    x.Purchase.UnitPrice,
                    x.Purchase.Quantity * x.Purchase.UnitPrice,
                    x.Purchase.UserId, // BuyerUserId
                    x.Purchase.CreationTime))
                .ToList();

            return (items, totalCount);
        }
    }
}
