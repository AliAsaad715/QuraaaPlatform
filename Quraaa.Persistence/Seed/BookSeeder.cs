using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Library;
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

            // التحقق من وجود الكتب مسبقاً لمنع التكرار عند إعادة تشغيل السيرفر
            if (await bookSet.AnyAsync(b => b.Title.StartsWith("Zero to One")))
            {
                return;
            }

            var targetLibraryId = Guid.Parse("01f185c0-dff4-45fa-8fe6-60d1c870ea8b");

            var categories = new[]
            {
                CategoryIds.Novels, CategoryIds.Technology, CategoryIds.Science,
                CategoryIds.History, CategoryIds.Literature, CategoryIds.Art
            };

            var arabicTitles = new[] { "أرض زيكولا", "مقدمة ابن خلدون", "ثلاثية غرناطة", "عابر سرير", "الخيميائي", "قواعد العشق الأربعون", "ساق البامبو", "الفيل الأزرق" };
            var englishTitles = new[] { "Clean Code", "The Pragmatic Programmer", "To Kill a Mockingbird", "180°C Knowledge", "Atomic Habits", "Dune", "The Hobbit", "Zero to One" };
            var authors = new[] { "Ahmed Khaled", "Robert C. Martin", "Radwa Ashour", "James Clear", "Naguib Mahfouz", "Dan Brown" };

            var books = new List<BookAggregate>();
            var listings = new List<ListingAggregate>();
            var random = new Random(42);

            for (int i = 0; i < 60; i++)
            {
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

                // 🌟 الحل هنا: استخدام العداد i ليكون الـ ISBN فريد 100% وحتمي
                var isbn = $"978-3-16-14{i:D4}-0";

                var book = new BookAggregate(
                    bookId,
                    title,
                    author,
                    description,
                    coverImage,
                    categoryId,
                    language,
                    isbn
                );

                books.Add(book);

                var listingId = Guid.NewGuid();
                var price = random.Next(10, 50) + 0.99m;
                var condition = (BookCondition)random.Next(0, 3);
                var stock = random.Next(2, 15);

                var listing = ListingAggregate.CreateForLibrary(
                    listingId,
                    bookId,
                    targetLibraryId,
                    price,
                    condition,
                    stock
                );

                listing.Approve(Guid.Empty);
                listings.Add(listing);
            }

            await bookSet.AddRangeAsync(books);
            await db.SaveChangesAsync();

            await listingSet.AddRangeAsync(listings);
            await db.SaveChangesAsync();
        }
    }
}