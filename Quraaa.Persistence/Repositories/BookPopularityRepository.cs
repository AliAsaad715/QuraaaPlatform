using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookPopularityRepository : IBookPopularityRepository
    {
        private readonly ApplicationDbContext _context;

        public BookPopularityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyCollection<PopularBookResponse> Items, int TotalCount)> GetMostPopularAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            string sortBy,
            bool includeUnranked,
            CancellationToken cancellationToken = default)
        {
            var purchaseStats =
                from purchase in _context.BookPurchases.AsNoTracking()
                where !purchase.IsDeleted
                group purchase by purchase.BookId into purchaseGroup
                select new
                {
                    BookId = purchaseGroup.Key,
                    PurchaseCount = purchaseGroup.Sum(purchase => (long)purchase.Quantity)
                };

            var ratingStats =
                from rating in _context.BookRatings.AsNoTracking()
                where !rating.IsDeleted
                group rating by rating.BookId into ratingGroup
                select new
                {
                    BookId = ratingGroup.Key,
                    RatingCount = ratingGroup.Count(),
                    AverageRating = ratingGroup.Average(rating => (double)rating.RatingValue)
                };

            var activeListingStats =
                from listing in _context.Listings.AsNoTracking()
                where listing.Status == ListingStatus.Active && !listing.IsDeleted
                group listing by listing.BookId into listingGroup
                select new
                {
                    BookId = listingGroup.Key,
                    ActiveListingCount = listingGroup.Count(),
                    LowestPrice = listingGroup.Min(listing => listing.Price)
                };

            var query =
                from book in _context.Books.AsNoTracking()
                where !book.IsDeleted
                join purchase in purchaseStats
                    on book.Id equals purchase.BookId into purchaseGroup
                from purchase in purchaseGroup.DefaultIfEmpty()
                join rating in ratingStats
                    on book.Id equals rating.BookId into ratingGroup
                from rating in ratingGroup.DefaultIfEmpty()
                join listing in activeListingStats
                    on book.Id equals listing.BookId into listingGroup
                from listing in listingGroup.DefaultIfEmpty()
                select new PopularBookFlatProjection
                {
                    BookId = book.Id,
                    Title = book.Title,
                    Author = book.Author,
                    Description = book.Description,
                    CoverImageUrl = book.CoverImageUrl,
                    CategoryId = book.CategoryId,
                    Language = book.Language,
                    Isbn = book.Isbn,
                    PurchaseCount = purchase == null ? 0 : purchase.PurchaseCount,
                    RatingCount = rating == null ? 0 : rating.RatingCount,
                    AverageRating = rating == null ? null : rating.AverageRating,
                    ActiveListingCount = listing == null ? 0 : listing.ActiveListingCount,
                    LowestPrice = listing == null ? null : listing.LowestPrice
                };

            if (!includeUnranked)
            {
                query = query.Where(book => book.PurchaseCount > 0 || book.RatingCount > 0);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(book =>
                    EF.Functions.ILike(book.Title, $"%{normalized}%") ||
                    EF.Functions.ILike(book.Author, $"%{normalized}%"));
            }

            query = ApplySorting(query, sortBy);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(book => new PopularBookResponse(
                    book.BookId,
                    book.Title,
                    book.Author,
                    book.Description,
                    book.CoverImageUrl,
                    book.CategoryId,
                    book.Language,
                    book.Isbn,
                    book.PurchaseCount,
                    book.RatingCount,
                    book.AverageRating,
                    book.ActiveListingCount,
                    book.LowestPrice))
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        private static IQueryable<PopularBookFlatProjection> ApplySorting(
            IQueryable<PopularBookFlatProjection> query,
            string sortBy)
        {
            return sortBy.ToLowerInvariant() switch
            {
                "purchases" => query
                    .OrderByDescending(book => book.PurchaseCount)
                    .ThenByDescending(book => book.RatingCount)
                    .ThenByDescending(book => book.AverageRating)
                    .ThenBy(book => book.Title),

                "ratings" => query
                    .OrderByDescending(book => book.RatingCount)
                    .ThenByDescending(book => book.AverageRating)
                    .ThenByDescending(book => book.PurchaseCount)
                    .ThenBy(book => book.Title),

                "averagerating" => query
                    .OrderByDescending(book => book.AverageRating.HasValue)
                    .ThenByDescending(book => book.AverageRating)
                    .ThenByDescending(book => book.RatingCount)
                    .ThenByDescending(book => book.PurchaseCount)
                    .ThenBy(book => book.Title),

                _ => query
                    .OrderByDescending(book => book.PurchaseCount)
                    .ThenByDescending(book => book.RatingCount)
                    .ThenByDescending(book => book.AverageRating)
                    .ThenBy(book => book.Title),
            };
        }

        private sealed class PopularBookFlatProjection
        {
            public Guid BookId { get; set; }
            public string Title { get; set; } = null!;
            public string Author { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string CoverImageUrl { get; set; } = null!;
            public Guid CategoryId { get; set; }
            public string Language { get; set; } = null!;
            public string? Isbn { get; set; }
            public long PurchaseCount { get; set; }
            public int RatingCount { get; set; }
            public double? AverageRating { get; set; }
            public int ActiveListingCount { get; set; }
            public decimal? LowestPrice { get; set; }
        }
    }
}
