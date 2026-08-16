using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Author;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed
{
    public static class EbookSeeder
    {
        private static readonly Guid EbookBookId = Guid.Parse("22222222-2222-2222-2222-222222222201");
        // 202 was historically seeded with an unstable user seller. Keep it
        // untouched in existing databases and use a new stable library-owned id.
        private static readonly Guid EbookListingId = Guid.Parse("22222222-2222-2222-2222-222222222203");

        private const string Title = "Ebook One";
        private const string Author = "Quraaa Seed Data";
        private const Language BookLanguage = Language.English;
        private const string DigitalAssetUrl = "books/book1.pdf";

        public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
        {
            var authorId = await context.Authors
                .Where(a => !a.IsDeleted && a.Name == Author)
                .OrderBy(a => a.CreationTime)
                .Select(a => (Guid?)a.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (authorId is null)
            {
                var newAuthor = new AuthorAggregate(Guid.NewGuid(), Author, null, null);
                await context.Authors.AddAsync(newAuthor, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                authorId = newAuthor.Id;
            }

            var matchingBooks = await context.Books
                .IgnoreQueryFilters()
                .Where(book =>
                    book.Id == EbookBookId ||
                    (book.Title == Title && book.AuthorId == authorId && book.Language == BookLanguage))
                .ToListAsync(cancellationToken);

            var byId = matchingBooks.FirstOrDefault(book => book.Id == EbookBookId);
            var byNaturalKey = matchingBooks.FirstOrDefault(book =>
                book.Title == Title &&
                book.AuthorId == authorId &&
                book.Language == BookLanguage);

            if (byId is not null &&
                (byId.Title != Title ||
                 byId.AuthorId != authorId ||
                 byId.Language != BookLanguage))
            {
                throw new InvalidOperationException(
                    $"Demo ebook id {EbookBookId} belongs to another catalog record.");
            }

            if (byId is not null &&
                byNaturalKey is not null &&
                byId.Id != byNaturalKey.Id)
            {
                throw new InvalidOperationException(
                    $"Demo ebook '{Title}' matches multiple catalog records.");
            }

            var bookId = (byId ?? byNaturalKey)?.Id ?? Guid.Empty;

            if (bookId == Guid.Empty)
            {
                var categoryId = await GetCategoryIdAsync(context, cancellationToken);
                if (categoryId == Guid.Empty)
                {
                    return;
                }

                var ebook = new BookAggregate(
                    EbookBookId,
                    Title,
                    authorId,
                    "Seeded ebook for development and manual testing.",
                    "/uploads/books/book1-cover.jpg",
                    BookLanguage,
                    categoryId);

                await context.Books.AddAsync(ebook, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                bookId = ebook.Id;
            }

            var sellerLibraryId = await context.Libraries
                .Where(library =>
                    !library.IsDeleted &&
                    library.ApprovalStatus == LibraryApprovalStatus.Approved)
                .OrderBy(library => library.Email == "info.lib1@quraaa.com" ? 0 : 1)
                .ThenBy(library => library.Email)
                .Select(library => (Guid?)library.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (sellerLibraryId is null)
            {
                return;
            }

            var existingListing = await context.Listings
                .FirstOrDefaultAsync(
                    listing => listing.Id == EbookListingId,
                    cancellationToken);

            if (existingListing is not null)
            {
                if (existingListing.BookId != bookId ||
                    existingListing.SellerType != SellerType.Library ||
                    existingListing.Format != ListingFormat.Digital ||
                    existingListing.LibraryId != sellerLibraryId ||
                    existingListing.UserId is not null)
                {
                    throw new InvalidOperationException(
                        $"Demo ebook listing id {EbookListingId} belongs to another record.");
                }

                if (existingListing.CustomDigitalAssetUrl != DigitalAssetUrl)
                {
                    existingListing.UpdateCustomDigitalAsset(
                        DigitalAssetUrl,
                        existingListing.LibraryId.Value);
                    existingListing.ClearDomainEvents();
                    await context.SaveChangesAsync(cancellationToken);
                }

                return;
            }

            var ebookListing = ListingAggregate.CreateDigitalForLibrary(
                EbookListingId,
                bookId,
                sellerLibraryId.Value,
                price: 1.00m,
                customDigitalAssetUrl: DigitalAssetUrl);
            ebookListing.ClearDomainEvents();

            await context.Listings.AddAsync(ebookListing, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task<Guid> GetCategoryIdAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            var categoryId = await context.Categories
                .Where(category => category.Id == CategoryIds.Technology)
                .Select(category => category.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (categoryId != Guid.Empty)
            {
                return categoryId;
            }

            return await context.Categories
                .OrderBy(category => category.Code)
                .Select(category => category.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
