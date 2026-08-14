using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookPopularityRepository : IBookPopularityRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public BookPopularityRepository(ApplicationDbContext context, IImageUrlFormatter imageUrlFormatter)
        {
            _context = context;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<(IReadOnlyCollection<PopularBookResponse> Items, int TotalCount)> GetMostPopularAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            string sortBy,
            bool includeUnranked,
            CancellationToken cancellationToken = default)
        {
            var query = BuildPopularityQuery();

            if (!includeUnranked)
            {
                query = query.Where(book => book.PurchaseCount > 0 || book.RatingCount > 0);
            }

            query = ApplySearch(query, searchTerm);

            var totalCount = await query.CountAsync(cancellationToken);

            query = ApplySorting(query, sortBy);

            var items = await ToPagedResponseListAsync(query, pageNumber, pageSize, cancellationToken);

            return (items, totalCount);
        }

        public async Task<(IReadOnlyCollection<PopularBookResponse> Items, int TotalCount)> GetRecommendedAsync(
            IReadOnlyCollection<Guid> interestedCategoryIds,
            string language,
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            var categoryIds = interestedCategoryIds
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

            if (categoryIds.Length == 0)
            {
                return (Array.Empty<PopularBookResponse>(), 0);
            }

            var normalizedLanguage = language.Trim().ToLowerInvariant();

            var query = BuildPopularityQuery()
                .Where(book =>
                    book.CategoryId.HasValue &&
                    categoryIds.Contains(book.CategoryId.Value) &&
                    book.Language.ToLower() == normalizedLanguage &&
                    book.ActiveListingCount > 0);

            query = ApplySearch(query, searchTerm);

            var totalCount = await query.CountAsync(cancellationToken);

            query = query
                .OrderByDescending(book => book.PurchaseCount)
                .ThenByDescending(book => book.RatingCount)
                .ThenByDescending(book => book.AverageRating.HasValue)
                .ThenByDescending(book => book.AverageRating)
                .ThenByDescending(book => book.ActiveListingCount)
                .ThenBy(book => book.Title)
                .ThenBy(book => book.BookId);

            var items = await ToPagedResponseListAsync(query, pageNumber, pageSize, cancellationToken);

            return (items, totalCount);
        }

        private IQueryable<PopularBookFlatProjection> BuildPopularityQuery()
        {
            var purchaseStats =
                from purchase in _context.BookPurchases.AsNoTracking()
                where !purchase.IsDeleted
                group purchase by purchase.BookId into purchaseGroup
                select new
                {
                    BookId = purchaseGroup.Key,
                    PurchaseCount = (long?)purchaseGroup.Sum(purchase => (long)purchase.Quantity)
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

            var activeListingStats =
                from listing in _context.Listings.AsNoTracking()
                where listing.Status == ListingStatus.Active && !listing.IsDeleted
                group listing by listing.BookId into listingGroup
                select new
                {
                    BookId = listingGroup.Key,
                    ActiveListingCount = (int?)listingGroup.Count()
                };

            return
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
                    PurchaseCount = purchase.PurchaseCount ?? 0,
                    RatingCount = rating.RatingCount ?? 0,
                    AverageRating = rating.AverageRating,
                    ActiveListingCount = listing.ActiveListingCount ?? 0
                };
        }

        private static IQueryable<PopularBookFlatProjection> ApplySearch(
            IQueryable<PopularBookFlatProjection> query,
            string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return query;
            }

            var normalized = searchTerm.Trim();
            return query.Where(book =>
                EF.Functions.ILike(book.Title, $"%{normalized}%") ||
                EF.Functions.ILike(book.Author, $"%{normalized}%"));
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

        private async Task<List<PopularBookResponse>> ToPagedResponseListAsync(
            IQueryable<PopularBookFlatProjection> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            // Materialize first, then project to the response DTO in memory —
            // IImageUrlFormatter.Format can't be translated into SQL.
            var projections = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return projections
                .Select(book => new PopularBookResponse(
                    book.BookId,
                    book.Title,
                    book.Author,
                    book.Description,
                    _imageUrlFormatter.Format(book.CoverImageUrl),
                    book.CategoryId,
                    book.Language,
                    book.Isbn,
                    book.PurchaseCount,
                    book.RatingCount,
                    book.AverageRating,
                    book.ActiveListingCount))
                .ToList();
        }

        private sealed class PopularBookFlatProjection
        {
            public Guid BookId { get; set; }
            public string Title { get; set; } = null!;
            public string Author { get; set; } = null!;
            public string Description { get; set; } = null!;
            public string CoverImageUrl { get; set; } = null!;
            public Guid? CategoryId { get; set; }
            public string Language { get; set; } = null!;
            public string? Isbn { get; set; }
            public long PurchaseCount { get; set; }
            public int RatingCount { get; set; }
            public double? AverageRating { get; set; }
            public int ActiveListingCount { get; set; }
        }
    }
}
