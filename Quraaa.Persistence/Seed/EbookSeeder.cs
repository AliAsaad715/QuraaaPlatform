using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed
{
    public static class EbookSeeder
    {
        private static readonly Guid EbookBookId = Guid.Parse("22222222-2222-2222-2222-222222222201");
        private static readonly Guid EbookListingId = Guid.Parse("22222222-2222-2222-2222-222222222202");

        private const string Title = "Ebook One";
        private const string Author = "Quraaa Seed Data";
        private const string Language = "en";
        private const string DigitalAssetUrl = "books/book1.pdf";

        public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
        {
            var bookId = await context.Books
                .Where(book =>
                    book.Id == EbookBookId ||
                    (book.Title == Title && book.Author == Author && book.Language == Language))
                .Select(book => book.Id)
                .FirstOrDefaultAsync(cancellationToken);

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
                    Author,
                    "Seeded ebook for development and manual testing.",
                    "/uploads/books/book1-cover.jpg",
                    Language,
                    categoryId);

                await context.Books.AddAsync(ebook, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                bookId = ebook.Id;
            }

            var existingListing = await context.Listings
                .Where(listing => listing.Id == EbookListingId || listing.DigitalAssetUrl == DigitalAssetUrl)
                .Select(listing => new { listing.Id, listing.DigitalAssetUrl })
                .FirstOrDefaultAsync(cancellationToken);

            if (existingListing is not null)
            {
                if (existingListing.Id == EbookListingId &&
                    existingListing.DigitalAssetUrl != DigitalAssetUrl)
                {
                    await context.Listings
                        .Where(listing => listing.Id == EbookListingId)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(
                                listing => listing.DigitalAssetUrl,
                                DigitalAssetUrl),
                            cancellationToken);
                }

                return;
            }

            var sellerUserId = await context.UsersProfiles
                .OrderBy(user => user.CreationTime)
                .Select(user => user.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (sellerUserId == Guid.Empty)
            {
                return;
            }

            var ebookListing = ListingAggregate.CreateForUser(
                EbookListingId,
                bookId,
                sellerUserId,
                ListingFormat.Digital,
                price: 1.00m,
                digitalAssetUrl: DigitalAssetUrl);

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
