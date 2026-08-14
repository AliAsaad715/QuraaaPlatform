using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Catalog.Common;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Features.Listings.Queries.GetListingById;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Category;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class ListingRepository : IListingRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public ListingRepository(ApplicationDbContext context, IImageUrlFormatter imageUrlFormatter)
        {
            _context = context;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<ListingAggregate?> GetByIdAsync(
            Guid listingId, CancellationToken cancellationToken = default) =>
            await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == ListingStatus.Active, cancellationToken);

        public async Task<ListingAggregate?> GetByIdForInventoryAsync(
            Guid listingId,
            CancellationToken cancellationToken = default) =>
            await _context.Listings
                .FirstOrDefaultAsync(
                    l => l.Id == listingId,
                    cancellationToken);

        public async Task<ListingDetailsResponse?> GetByIdWithDetailsAsync(
        Guid listingId, CancellationToken cancellationToken = default)
        {
            // Materialize first, then project to the response DTO in memory —
            // IImageUrlFormatter.Format can't be translated into SQL.
            var row = await _context.Listings
                .AsNoTracking()
                .Where(l => l.Id == listingId)
                .Join(
                    _context.Books.AsNoTracking(),
                    l => l.BookId,
                    b => b.Id,
                    (l, b) => new { Listing = l, Book = b })
                .GroupJoin(
                    _context.Categories.AsNoTracking(),
                    x => x.Book.CategoryId,
                    c => c.Id,
                    (x, categories) => new { x.Listing, x.Book, Categories = categories })
                .SelectMany(
                    x => x.Categories.DefaultIfEmpty(),
                    (x, c) => new { x.Listing, x.Book, Category = c })
                .FirstOrDefaultAsync(cancellationToken);

            if (row is null)
            {
                return null;
            }

            return new ListingDetailsResponse(
                row.Listing.Id,
                row.Listing.Price,
                row.Listing.Stock,
                row.Listing.Condition,
                row.Listing.Status,
                new BookDetails(
                    row.Book.Id,
                    row.Book.Title,
                    row.Book.Author,
                    row.Book.Description,
                    _imageUrlFormatter.Format(row.Book.CoverImageUrl),
                    row.Book.Language,
                    row.Book.Isbn,
                    row.Category == null ? null : new CategoryResponse(row.Category.Id, row.Category.NameEn, row.Category.NameAr)));
        }

        public async Task<bool> ExistsByLibraryAndBookAsync(
            Guid libraryId, Guid bookId, CancellationToken cancellationToken = default) =>
            await _context.Listings
                .AnyAsync(l => l.LibraryId == libraryId && l.BookId == bookId,
                    cancellationToken);

        public async Task<bool> ExistsByUserAndBookAsync(
            Guid userId,
            Guid bookId,
            CancellationToken cancellationToken = default) =>
            await _context.Listings
                .AnyAsync(l => l.UserId == userId && l.BookId == bookId,
                    cancellationToken);

        public async Task<(IReadOnlyCollection<ListingSummaryResponse> Items, int TotalCount)> GetUserBooksForSaleAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Listings
                .AsNoTracking()
                .Where(lb => lb.UserId == userId && lb.SellerType == SellerType.User && lb.Status == ListingStatus.Active)
                .Join(
                    _context.Books.AsNoTracking(),
                    lb => lb.BookId,
                    b => b.Id,
                    (lb, b) => new { Listing = lb, Book = b })
                .GroupJoin(
                    _context.Categories.AsNoTracking(),
                    x => x.Book.CategoryId,
                    c => c.Id,
                    (x, categories) => new { x.Listing, x.Book, Categories = categories })
                .SelectMany(
                    x => x.Categories.DefaultIfEmpty(),
                    (x, c) => new UserListingFlatProjection
                    {
                        Listing = x.Listing,
                        Book = x.Book,
                        Category = c
                    });

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(x =>
                    EF.Functions.ILike(x.Book.Title, $"%{normalized}%") ||
                    EF.Functions.ILike(x.Book.Author, $"%{normalized}%") ||
                    EF.Functions.ILike(x.Book.Language, $"%{normalized}%"));
            }

            query = ApplySorting(query, sortBy, sortDescending);

            var totalCount = await query.CountAsync(cancellationToken);

            // Materialize first, then project to the response DTO in memory —
            // IImageUrlFormatter.Format can't be translated into SQL.
            var rows = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = rows
                .Select(x => new ListingSummaryResponse(
                    x.Listing.Id,
                    x.Listing.Price,
                    x.Listing.Stock,
                    x.Listing.Condition,
                    x.Listing.Status,
                    new BookDetails(
                        x.Book.Id,
                        x.Book.Title,
                        x.Book.Author,
                        x.Book.Description,
                        _imageUrlFormatter.Format(x.Book.CoverImageUrl),
                        x.Book.Language,
                        x.Book.Isbn,
                        x.Category == null
                            ? null
                            : new CategoryResponse(x.Category.Id, x.Category.NameEn, x.Category.NameAr))))
                .ToList();

            return (items, totalCount);
        }

        public async Task AddAsync(
            ListingAggregate listing, CancellationToken cancellationToken = default) =>
            await _context.Listings.AddAsync(listing, cancellationToken);

        public async Task<HashSet<string>> FilterReferencedDigitalAssetPathsAsync(
            IReadOnlyCollection<string> relativePaths,
            CancellationToken cancellationToken = default)
        {
            if (relativePaths.Count == 0)
                return [];

            var referenced = await _context.Listings
                .AsNoTracking()
                .Where(l => l.CustomDigitalAssetUrl != null && relativePaths.Contains(l.CustomDigitalAssetUrl))
                .Select(l => l.CustomDigitalAssetUrl!)
                .ToListAsync(cancellationToken);

            return referenced.ToHashSet(StringComparer.Ordinal);
        }

        public async Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Listing changed concurrently. Reload it and retry the operation.");
            }
        }

        private static IQueryable<UserListingFlatProjection> ApplySorting(
            IQueryable<UserListingFlatProjection> query,
            string? sortBy,
            bool sortDescending)
        {
            return sortBy?.ToLowerInvariant() switch
            {
                "author" => sortDescending ? query.OrderByDescending(x => x.Book.Author) : query.OrderBy(x => x.Book.Author),
                "quantity" => sortDescending ? query.OrderByDescending(x => x.Listing.Stock) : query.OrderBy(x => x.Listing.Stock),
                _ => sortDescending ? query.OrderByDescending(x => x.Book.Title) : query.OrderBy(x => x.Book.Title),
            };
        }

        private sealed class UserListingFlatProjection
        {
            public required ListingAggregate Listing { get; set; }
            public required BookAggregate Book { get; set; }
            public CategoryAggregate? Category { get; set; }
        }
    }
}
