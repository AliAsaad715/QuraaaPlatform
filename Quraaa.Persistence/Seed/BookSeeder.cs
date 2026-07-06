using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Quraaa.Persistence.Seed
{
    public static class BookSeeder
    {
        public static async Task SeedAsync(DbContext db)
        {
            var bookSet = db.Set<BookAggregate>();
            var listingSet = db.Set<ListingAggregate>();
            var librarySet = db.Set<LibraryAggregate>();

            var targetLibraryId = await librarySet
                .AsNoTracking()
                .Where(l => l.ApprovalStatus == LibraryApprovalStatus.Approved)
                .OrderBy(l => l.LibraryName)
                .Select(l => (Guid?)l.Id)
                .FirstOrDefaultAsync();

            targetLibraryId ??= await librarySet
                .AsNoTracking()
                .OrderBy(l => l.LibraryName)
                .Select(l => (Guid?)l.Id)
                .FirstOrDefaultAsync();

            if (targetLibraryId is null)
            {
                return;
            }

            var categories = new[]
            {
                CategoryIds.Novels, CategoryIds.Technology, CategoryIds.Science,
                CategoryIds.History, CategoryIds.Literature, CategoryIds.Art
            };

            var arabicTitles = new[] { "أرض زيكولا", "مقدمة ابن خلدون", "ثلاثية غرناطة", "عابر سرير", "الخيميائي", "قواعد العشق الأربعون", "ساق البامبو", "الفيل الأزرق" };
            var englishTitles = new[] { "Clean Code", "The Pragmatic Programmer", "To Kill a Mockingbird", "180°C Knowledge", "Atomic Habits", "Dune", "The Hobbit", "Zero to One" };
            var authors = new[] { "Ahmed Khaled", "Robert C. Martin", "Radwa Ashour", "James Clear", "Naguib Mahfouz", "Dan Brown" };

            var seedIsbns = Enumerable.Range(0, 60)
                .Select(i => $"978-3-16-14{i:D4}-0")
                .ToArray();

            var seedBookIdsByIsbn = await bookSet
                .Where(b => b.Isbn != null && seedIsbns.Contains(b.Isbn))
                .ToDictionaryAsync(b => b.Isbn!, b => b.Id);

            var books = new List<BookAggregate>();

            for (int i = 0; i < 60; i++)
            {
                var isbn = seedIsbns[i];

                if (seedBookIdsByIsbn.ContainsKey(isbn))
                {
                    continue;
                }

                var bookId = Guid.NewGuid();
                bool isArabic = i % 2 == 0;

                var title = isArabic
                    ? $"{arabicTitles[i % arabicTitles.Length]} (نسخة {i})"
                    : $"{englishTitles[i % englishTitles.Length]} (Seed {i})";

                var author = authors[i % authors.Length];
                var description = isArabic
                    ? $"هذا الكتاب يعتبر من أفضل المؤلفات في مجاله، يقدم نظرة عميقة وشاملة للمواضيع المطروحة بأسلوب شيق."
                    : $"An exceptional masterpiece that dives deep into the core concepts, providing readers with practical insights and invaluable knowledge.";

                var coverImage = isArabic
                    ? "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=500&q=80"
                    : "https://images.unsplash.com/photo-1543002588-bfa74002ed7e?w=500&q=80";

                var categoryId = categories[i % categories.Length];
                var language = isArabic ? "ar" : "en";

                var book = new BookAggregate(
                    bookId,
                    title,
                    author,
                    description,
                    coverImage,
                    language,
                    categoryId,
                    isbn
                );

                books.Add(book);
                seedBookIdsByIsbn.Add(isbn, bookId);
            }

            if (books.Count > 0)
            {
                await bookSet.AddRangeAsync(books);
                await db.SaveChangesAsync();
            }

            var seedBookIds = seedBookIdsByIsbn.Values.ToArray();
            var listedBookIds = (await listingSet
                .Where(l => seedBookIds.Contains(l.BookId))
                .Select(l => l.BookId)
                .ToListAsync())
                .ToHashSet();

            var listings = new List<ListingAggregate>();
            var random = new Random(42);

            for (int i = 0; i < 60; i++)
            {
                var bookId = seedBookIdsByIsbn[seedIsbns[i]];
                var price = random.Next(10, 50) + 0.99m;
                var condition = (BookCondition)random.Next(0, 3);
                var stock = random.Next(2, 15);

                if (listedBookIds.Contains(bookId))
                {
                    continue;
                }

                var listingId = Guid.NewGuid();
                var listing = ListingAggregate.CreateForLibrary(
                    listingId,
                    bookId,
                    targetLibraryId.Value,
                    price,
                    condition,
                    stock
                );

                listing.Approve(Guid.Empty);
                listings.Add(listing);
            }

            if (listings.Count > 0)
            {
                await listingSet.AddRangeAsync(listings);
                await db.SaveChangesAsync();
            }
        }
    }
}
