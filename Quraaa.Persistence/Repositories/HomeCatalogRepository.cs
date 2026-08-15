using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class HomeCatalogRepository : IHomeCatalogRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public HomeCatalogRepository(ApplicationDbContext context, IImageUrlFormatter imageUrlFormatter)
        {
            _context = context;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<(IReadOnlyCollection<HomeBookResponse> Items, int TotalCount)> GetCatalogAsync(
            string? searchTerm,
            Guid? categoryId,
            Guid? libraryId,
            SellerType? sellerType,
            ListingFormat? format,
            bool? isFree,
            BookCondition? condition,
            decimal? minPrice,
            decimal? maxPrice,
            string sortBy,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = BuildCatalogQuery(libraryId, sellerType, format, condition, minPrice, maxPrice);

            if (categoryId.HasValue)
            {
                query = query.Where(book => book.CategoryId == categoryId.Value);
            }

            query = ApplySearch(query, searchTerm);

            if (isFree.HasValue)
            {
                query = isFree.Value
                    ? query.Where(book => book.StartingPrice == 0)
                    : query.Where(book => book.StartingPrice > 0);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplySorting(query, sortBy);

            var items = await ToPagedResponseListAsync(query, pageNumber, pageSize, cancellationToken);

            return (items, totalCount);
        }

        // A book qualifies only when it has at least one active listing with available
        // stock (inner join to listingStats below). There is currently no domain concept
        // of a free-to-read book outside a priced listing, and every listing requires
        // Price > 0, so IsFree filtering is honored but will not match any data today.
        private IQueryable<HomeBookFlatProjection> BuildCatalogQuery(
            Guid? libraryId,
            SellerType? sellerType,
            ListingFormat? format,
            BookCondition? condition,
            decimal? minPrice,
            decimal? maxPrice)
        {
            var filteredListings = _context.Listings.AsNoTracking()
                .Where(listing => !listing.IsDeleted
                    && listing.Status == ListingStatus.Active
                    && (listing.Stock ?? 0) > 0);

            if (libraryId.HasValue)
            {
                filteredListings = filteredListings.Where(listing => listing.LibraryId == libraryId.Value);
            }

            if (sellerType.HasValue)
            {
                filteredListings = filteredListings.Where(listing => listing.SellerType == sellerType.Value);
            }

            if (format.HasValue)
            {
                filteredListings = filteredListings.Where(listing => listing.Format == format.Value);
            }

            if (condition.HasValue)
            {
                filteredListings = filteredListings.Where(listing => listing.Condition == condition.Value);
            }

            if (minPrice.HasValue)
            {
                filteredListings = filteredListings.Where(listing => listing.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                filteredListings = filteredListings.Where(listing => listing.Price <= maxPrice.Value);
            }

            var listingStats =
                from listing in filteredListings
                group listing by listing.BookId into listingGroup
                select new
                {
                    BookId = listingGroup.Key,
                    StartingPrice = listingGroup.Min(listing => listing.Price),
                    DigitalCount = listingGroup.Count(listing => listing.Format == ListingFormat.Digital),
                    PhysicalCount = listingGroup.Count(listing => listing.Format == ListingFormat.Physical)
                };

            var ratingStats =
                from rating in _context.BookRatings.AsNoTracking()
                where !rating.IsDeleted
                group rating by rating.BookId into ratingGroup
                select new
                {
                    BookId = ratingGroup.Key,
                    RatingCount = (int?)ratingGroup.Count(),
                    AverageRating = ratingGroup.Average(rating => (double?)rating.RatingValue)
                };

            var purchaseStats =
                from purchase in _context.BookPurchases.AsNoTracking()
                where !purchase.IsDeleted
                group purchase by purchase.BookId into purchaseGroup
                select new
                {
                    BookId = purchaseGroup.Key,
                    PurchaseCount = (long?)purchaseGroup.Sum(purchase => (long)purchase.Quantity)
                };

            return
                from book in _context.Books.AsNoTracking()
                where !book.IsDeleted
                join stats in listingStats
                    on book.Id equals stats.BookId
                join category in _context.Categories.AsNoTracking()
                    on book.CategoryId equals category.Id into categoryJoin
                from category in categoryJoin.DefaultIfEmpty()
                join author in _context.Authors.AsNoTracking()
                    on book.AuthorId equals author.Id into authorJoin
                from author in authorJoin.DefaultIfEmpty()
                join rating in ratingStats
                    on book.Id equals rating.BookId into ratingJoin
                from rating in ratingJoin.DefaultIfEmpty()
                join purchase in purchaseStats
                    on book.Id equals purchase.BookId into purchaseJoin
                from purchase in purchaseJoin.DefaultIfEmpty()
                select new HomeBookFlatProjection
                {
                    BookId = book.Id,
                    Title = book.Title,
                    AuthorName = author.Name,
                    CoverImageUrl = book.CoverImageUrl,
                    CategoryId = book.CategoryId,
                    CategoryNameAr = category.NameAr,
                    CategoryNameEn = category.NameEn,
                    CreationTimeUtc = book.CreationTime,
                    StartingPrice = stats.StartingPrice,
                    DigitalListingCount = stats.DigitalCount,
                    PhysicalListingCount = stats.PhysicalCount,
                    RatingsCount = rating.RatingCount ?? 0,
                    AverageRating = rating.AverageRating,
                    PurchaseCount = purchase.PurchaseCount ?? 0
                };
        }

        private static IQueryable<HomeBookFlatProjection> ApplySearch(
            IQueryable<HomeBookFlatProjection> query,
            string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return query;
            }

            var normalized = searchTerm.Trim();
            return query.Where(book =>
                EF.Functions.ILike(book.Title, $"%{normalized}%") ||
                EF.Functions.ILike(book.AuthorName!, $"%{normalized}%"));
        }

        private static IQueryable<HomeBookFlatProjection> ApplySorting(
            IQueryable<HomeBookFlatProjection> query,
            string sortBy)
        {
            return sortBy.ToLowerInvariant() switch
            {
                "bestselling" or "mostpopular" => query
                    .OrderByDescending(book => book.PurchaseCount)
                    .ThenByDescending(book => book.AverageRating.HasValue)
                    .ThenByDescending(book => book.AverageRating)
                    .ThenBy(book => book.BookId),

                "pricelowtohigh" => query
                    .OrderBy(book => book.StartingPrice)
                    .ThenBy(book => book.BookId),

                "pricehightolow" => query
                    .OrderByDescending(book => book.StartingPrice)
                    .ThenBy(book => book.BookId),

                "toprated" => query
                    .OrderByDescending(book => book.AverageRating.HasValue)
                    .ThenByDescending(book => book.AverageRating)
                    .ThenByDescending(book => book.RatingsCount)
                    .ThenBy(book => book.BookId),

                // "latest" and any unrecognized value (validator already restricts input)
                _ => query
                    .OrderByDescending(book => book.CreationTimeUtc)
                    .ThenBy(book => book.BookId),
            };
        }

        private async Task<List<HomeBookResponse>> ToPagedResponseListAsync(
            IQueryable<HomeBookFlatProjection> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            // Materialize first, then format the cover image URL and resolve the
            // display format in memory — IImageUrlFormatter.Format can't be
            // translated into SQL.
            var projections = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return projections
                .Select(book => new HomeBookResponse(
                    book.BookId,
                    book.Title,
                    book.AuthorName,
                    _imageUrlFormatter.Format(book.CoverImageUrl),
                    book.CategoryId is null
                        ? null
                        : new CategoryResponse(book.CategoryId.Value, book.CategoryNameAr ?? string.Empty, book.CategoryNameEn ?? string.Empty),
                    book.DigitalListingCount > 0 ? ListingFormat.Digital : ListingFormat.Physical,
                    book.StartingPrice,
                    book.StartingPrice == 0,
                    book.DigitalListingCount + book.PhysicalListingCount,
                    book.AverageRating,
                    book.RatingsCount))
                .ToList();
        }

        private sealed class HomeBookFlatProjection
        {
            public Guid BookId { get; set; }
            public string Title { get; set; } = null!;
            public string? AuthorName { get; set; }
            public string CoverImageUrl { get; set; } = null!;
            public Guid? CategoryId { get; set; }
            public string? CategoryNameAr { get; set; }
            public string? CategoryNameEn { get; set; }
            public DateTime CreationTimeUtc { get; set; }
            public decimal StartingPrice { get; set; }
            public int DigitalListingCount { get; set; }
            public int PhysicalListingCount { get; set; }
            public int RatingsCount { get; set; }
            public double? AverageRating { get; set; }
            public long PurchaseCount { get; set; }
        }
    }
}
