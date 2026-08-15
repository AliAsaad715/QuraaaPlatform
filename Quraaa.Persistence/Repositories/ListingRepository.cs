using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Catalog.Common;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Features.Listings.Queries.GetListingById;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Author;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Category;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;
// Aliased instead of a plain `using`: GetListingDetails also declares a "ListingDetailsResponse",
// which would otherwise collide with the GetListingById one already imported above.
using MobileListingDetailsResponse = Quraaa.Application.Features.Listings.Queries.GetListingDetails.ListingDetailsResponse;
using BookReviewDto = Quraaa.Application.Features.Listings.Queries.GetListingDetails.BookReviewDto;

namespace Quraaa.Persistence.Repositories
{
    public class ListingRepository : IListingRepository
    {
        private const int RecentReviewsLimit = 5;

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

        public async Task<IReadOnlyDictionary<Guid, ListingFormat>> GetActiveFormatsByIdsAsync(
            IReadOnlyCollection<Guid> listingIds,
            CancellationToken cancellationToken = default)
        {
            if (listingIds.Count == 0)
            {
                return new Dictionary<Guid, ListingFormat>();
            }

            var formats = await _context.Listings
                .AsNoTracking()
                .Where(listing =>
                    listingIds.Contains(listing.Id)
                    && listing.Status == ListingStatus.Active)
                .Select(listing => new
                {
                    listing.Id,
                    listing.Format
                })
                .ToListAsync(cancellationToken);

            return formats.ToDictionary(listing => listing.Id, listing => listing.Format);
        }

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
                .GroupJoin(
                    _context.Authors.AsNoTracking(),
                    x => x.Book.AuthorId,
                    a => a.Id,
                    (x, authors) => new { x.Listing, x.Book, x.Category, Authors = authors })
                .SelectMany(
                    x => x.Authors.DefaultIfEmpty(),
                    (x, a) => new { x.Listing, x.Book, x.Category, Author = a })
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
                row.Listing.Format,
                row.Listing.Version,
                new BookDetails(
                    row.Book.Id,
                    row.Book.Title,
                    row.Author?.Name,
                    row.Book.Description,
                    _imageUrlFormatter.Format(row.Book.CoverImageUrl),
                    row.Book.Language,
                    row.Book.Isbn,
                    row.Category == null ? null : new CategoryResponse(row.Category.Id, row.Category.NameEn, row.Category.NameAr)));
        }

        public async Task<MobileListingDetailsResponse?> GetListingDetailsAsync(
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
                    _context.Authors.AsNoTracking(),
                    x => x.Book.AuthorId,
                    a => a.Id,
                    (x, authors) => new { x.Listing, x.Book, Authors = authors })
                .SelectMany(
                    x => x.Authors.DefaultIfEmpty(),
                    (x, a) => new { x.Listing, x.Book, Author = a })
                .GroupJoin(
                    _context.Libraries.AsNoTracking(),
                    x => x.Listing.LibraryId,
                    lib => lib.Id,
                    (x, libraries) => new { x.Listing, x.Book, x.Author, Libraries = libraries })
                .SelectMany(
                    x => x.Libraries.DefaultIfEmpty(),
                    (x, lib) => new { x.Listing, x.Book, x.Author, Library = lib })
                .GroupJoin(
                    _context.UsersProfiles.AsNoTracking(),
                    x => x.Listing.UserId,
                    u => u.Id,
                    (x, users) => new { x.Listing, x.Book, x.Author, x.Library, Users = users })
                .SelectMany(
                    x => x.Users.DefaultIfEmpty(),
                    (x, u) => new { x.Listing, x.Book, x.Author, x.Library, Seller = u })
                .FirstOrDefaultAsync(cancellationToken);

            if (row is null)
            {
                return null;
            }

            var (averageRating, totalReviewsCount) = await GetRatingSummaryAsync(row.Book.Id, cancellationToken);
            var recentReviews = await GetRecentReviewsAsync(row.Book.Id, cancellationToken);

            // LibraryId/UserId are FK-enforced, so Library/Seller are guaranteed non-null
            // whenever SellerType points at them.
            var publisher = row.Listing.SellerType == SellerType.Library
                ? row.Library!.LibraryName
                : $"{row.Seller!.FirstName} {row.Seller!.LastName}";

            return new MobileListingDetailsResponse(
                row.Listing.Id,
                row.Book.Title,
                _imageUrlFormatter.Format(row.Book.CoverImageUrl),
                row.Listing.Format,
                row.Listing.Condition,
                row.Book.Language,
                publisher,
                row.Author?.Name ?? string.Empty,
                row.Listing.Version,
                row.Listing.Price,
                new List<string>(),
                averageRating,
                totalReviewsCount,
                recentReviews);
        }

        private async Task<(double AverageRating, int TotalReviewsCount)> GetRatingSummaryAsync(
            Guid bookId, CancellationToken cancellationToken)
        {
            var ratings = _context.BookRatings
                .AsNoTracking()
                .Where(r => r.BookId == bookId && !r.IsDeleted);

            var totalCount = await ratings.CountAsync(cancellationToken);
            if (totalCount == 0)
            {
                return (0, 0);
            }

            var average = await ratings.AverageAsync(r => (double)r.RatingValue, cancellationToken);
            return (average, totalCount);
        }

        private async Task<List<BookReviewDto>> GetRecentReviewsAsync(
            Guid bookId, CancellationToken cancellationToken)
        {
            return await _context.Comments
                .AsNoTracking()
                .Where(c => c.BookId == bookId && !c.IsDeleted)
                .Join(
                    _context.UsersProfiles.AsNoTracking(),
                    c => c.UserId,
                    u => u.Id,
                    (c, u) => new { Comment = c, User = u })
                .GroupJoin(
                    _context.BookRatings.AsNoTracking().Where(r => r.BookId == bookId && !r.IsDeleted),
                    x => x.Comment.UserId,
                    r => r.UserId,
                    (x, ratings) => new { x.Comment, x.User, Ratings = ratings })
                .SelectMany(
                    x => x.Ratings.DefaultIfEmpty(),
                    (x, r) => new { x.Comment, x.User, Rating = r })
                .OrderByDescending(x => x.Comment.CreationTime)
                .Take(RecentReviewsLimit)
                .Select(x => new BookReviewDto(
                    x.Rating == null ? (int?)null : x.Rating.RatingValue,
                    x.Comment.Content,
                    x.User.FirstName + " " + x.User.LastName,
                    x.Comment.CreationTime))
                .ToListAsync(cancellationToken);
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
                    (x, c) => new { x.Listing, x.Book, Category = c })
                .GroupJoin(
                    _context.Authors.AsNoTracking(),
                    x => x.Book.AuthorId,
                    a => a.Id,
                    (x, authors) => new { x.Listing, x.Book, x.Category, Authors = authors })
                .SelectMany(
                    x => x.Authors.DefaultIfEmpty(),
                    (x, a) => new UserListingFlatProjection
                    {
                        Listing = x.Listing,
                        Book = x.Book,
                        Category = x.Category,
                        Author = a
                    });

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(x =>
                    EF.Functions.ILike(x.Book.Title, $"%{normalized}%") ||
                    EF.Functions.ILike(x.Author!.Name, $"%{normalized}%"));
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
                    x.Listing.Version,
                    new BookDetails(
                        x.Book.Id,
                        x.Book.Title,
                        x.Author?.Name,
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
                "author" => sortDescending ? query.OrderByDescending(x => x.Author!.Name) : query.OrderBy(x => x.Author!.Name),
                "quantity" => sortDescending ? query.OrderByDescending(x => x.Listing.Stock) : query.OrderBy(x => x.Listing.Stock),
                _ => sortDescending ? query.OrderByDescending(x => x.Book.Title) : query.OrderBy(x => x.Book.Title),
            };
        }

        private sealed class UserListingFlatProjection
        {
            public required ListingAggregate Listing { get; set; }
            public required BookAggregate Book { get; set; }
            public CategoryAggregate? Category { get; set; }
            public AuthorAggregate? Author { get; set; }
        }
    }
}
