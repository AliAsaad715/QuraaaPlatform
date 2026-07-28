using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Ebooks.Common;
using Quraaa.Application.Features.Ebooks.Interfaces;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class EbookRepository : IEbookRepository
    {
        private readonly ApplicationDbContext _context;

        public EbookRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(IReadOnlyCollection<EbookResponse> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            var query =
                from listing in _context.Listings.AsNoTracking()
                join book in _context.Books.AsNoTracking()
                    on listing.BookId equals book.Id
                where listing.Format == ListingFormat.Digital
                    && listing.Status == ListingStatus.Active
                    && listing.DigitalAssetUrl != null
                select new
                {
                    listing.Id,
                    listing.BookId,
                    book.Title,
                    book.Author,
                    book.Description,
                    book.CoverImageUrl,
                    book.CategoryId,
                    book.Language,
                    book.Isbn,
                    listing.Price
                };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim().ToLower();
                query = query.Where(ebook =>
                    ebook.Title.ToLower().Contains(normalized) ||
                    ebook.Author.ToLower().Contains(normalized));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(ebook => ebook.Title)
                .ThenBy(ebook => ebook.Author)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(ebook => new EbookResponse(
                    ebook.Id,
                    ebook.BookId,
                    ebook.Title,
                    ebook.Author,
                    ebook.Description,
                    ebook.CoverImageUrl,
                    ebook.CategoryId ?? Guid.Empty,
                    ebook.Language,
                    ebook.Isbn,
                    ebook.Price))
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }
    }
}
