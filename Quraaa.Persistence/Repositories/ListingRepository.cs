using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Categories.Common;
using Quraaa.Application.Features.Listings.Common;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Listings.Queries.GetListingById;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class ListingRepository : IListingRepository
    {
        private readonly ApplicationDbContext _context;

        public ListingRepository(ApplicationDbContext context) => _context = context;

        public async Task<ListingAggregate?> GetByIdAsync(
            Guid listingId, CancellationToken cancellationToken = default) =>
            await _context.Listings
                .FirstOrDefaultAsync(l => l.Id == listingId && l.Status == ListingStatus.Active, cancellationToken);

        public async Task<ListingDetailsResponse?> GetByIdWithDetailsAsync(
        Guid listingId, CancellationToken cancellationToken = default) =>
            await _context.Listings
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
                    (x, c) => new ListingDetailsResponse(
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
                            x.Book.CoverImageUrl,
                            x.Book.Language,
                            x.Book.Isbn,
                            c == null ? null : new CategoryResponse(c.Id, c.NameEn, c.NameAr))))
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<bool> ExistsByLibraryAndBookAsync(
            Guid libraryId, Guid bookId, CancellationToken cancellationToken = default) =>
            await _context.Listings
                .AnyAsync(l => l.LibraryId == libraryId && l.BookId == bookId,
                    cancellationToken);

        public async Task AddAsync(
            ListingAggregate listing, CancellationToken cancellationToken = default) =>
            await _context.Listings.AddAsync(listing, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);
    }
}