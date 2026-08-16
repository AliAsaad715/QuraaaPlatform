using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Comments;
using Quraaa.Domain.Favorites;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Ratings;
using Quraaa.Domain.Reports;
using Quraaa.Domain.Reports.Enums;
using Quraaa.Domain.Purchases;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed;

public static class DemoEngagementSeeder
{
    private sealed record UserBookSeed(string PhoneNumber, Guid BookId, int? Rating = null);
    private sealed record CommentSeed(string PhoneNumber, Guid BookId, string Content, int DaysAgo);
    private sealed record ReportSeed(
        string PhoneNumber,
        Guid BookId,
        BookReportReason Reason,
        string Details,
        BookReportStatus Status);

    public static async Task SeedAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<Guid, Guid> books,
        CancellationToken cancellationToken = default)
    {
        var users = await GetUsersAsync(context, cancellationToken);
        var listings = await GetListingsAsync(context, cancellationToken);

        var expectedBookKeys = new[]
        {
            DemoSeedData.Books.Granada,
            DemoSeedData.Books.Utopia,
            DemoSeedData.Books.Muqaddimah,
            DemoSeedData.Books.CleanCode,
            DemoSeedData.Books.Dune,
            DemoSeedData.Books.AtomicHabits,
            DemoSeedData.Books.PragmaticProgrammer,
            DemoSeedData.Books.ModerationFlagged,
            DemoSeedData.Books.ModerationHidden,
            DemoSeedData.Books.LePetitPrince,
        };
        if (expectedBookKeys.Any(key => !books.ContainsKey(key)))
        {
            throw new InvalidOperationException(
                "One or more demo engagement book mappings are missing.");
        }

        await EnsurePurchaseHistoryAsync(
            context,
            users,
            books,
            listings,
            cancellationToken);
        await EnsureFavoritesAsync(context, users, books, cancellationToken);
        await EnsureRatingsAsync(context, users, books, cancellationToken);
        await EnsureCommentsAsync(context, users, books, cancellationToken);
        await EnsureReportsAsync(context, users, books, cancellationToken);
    }

    private static async Task EnsurePurchaseHistoryAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Guid> users,
        IReadOnlyDictionary<Guid, Guid> books,
        IReadOnlyDictionary<Guid, ListingAggregate> listings,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new UserBookSeed(DemoSeedData.SellerPhoneNumber, DemoSeedData.Books.CleanCode),
            new UserBookSeed(DemoSeedData.SellerPhoneNumber, DemoSeedData.Books.Dune),
            new UserBookSeed(DemoSeedData.CheckoutBuyerPhoneNumber, DemoSeedData.Books.Dune),
            new UserBookSeed(DemoSeedData.CheckoutBuyerPhoneNumber, DemoSeedData.Books.AtomicHabits),
            new UserBookSeed(DemoSeedData.ReporterOnePhoneNumber, DemoSeedData.Books.CleanCode),
            new UserBookSeed(DemoSeedData.ReporterTwoPhoneNumber, DemoSeedData.Books.Granada),
            new UserBookSeed(DemoSeedData.ReporterTwoPhoneNumber, DemoSeedData.Books.Dune),
            new UserBookSeed(DemoSeedData.ReporterThreePhoneNumber, DemoSeedData.Books.CleanCode),
            new UserBookSeed(DemoSeedData.ReporterThreePhoneNumber, DemoSeedData.Books.Dune),
        };

        var listingIdByBookSeedId = new Dictionary<Guid, Guid>
        {
            [DemoSeedData.Books.Granada] = DemoSeedData.Listings.GranadaLibrary,
            [DemoSeedData.Books.CleanCode] = DemoSeedData.Listings.CleanCodeLibraryOne,
            [DemoSeedData.Books.Dune] = DemoSeedData.Listings.DuneDigital,
            [DemoSeedData.Books.AtomicHabits] = DemoSeedData.Listings.AtomicHabitsOutOfStock,
        };

        var userIds = definitions.Select(definition => users[definition.PhoneNumber]).Distinct().ToArray();
        var bookIds = definitions.Select(definition => books[definition.BookId]).Distinct().ToArray();
        var existingPairs = await context.BookPurchases
            .Where(purchase =>
                userIds.Contains(purchase.UserId) &&
                bookIds.Contains(purchase.BookId))
            .Select(purchase => new { purchase.UserId, purchase.BookId })
            .ToListAsync(cancellationToken);
        var pairSet = existingPairs
            .Select(pair => (pair.UserId, pair.BookId))
            .ToHashSet();

        var daysAgo = 45;
        foreach (var definition in definitions)
        {
            var userId = users[definition.PhoneNumber];
            var bookId = books[definition.BookId];
            if (pairSet.Contains((userId, bookId)))
            {
                continue;
            }

            var listing = listings[listingIdByBookSeedId[definition.BookId]];
            var purchase = BookPurchaseAggregate.Create(
                userId,
                bookId,
                listing.Id,
                quantity: 1,
                unitPrice: listing.Price,
                purchasedDigitalAssetUrl: listing.CustomDigitalAssetUrl);

            await context.BookPurchases.AddAsync(purchase, cancellationToken);
            SetCreationTime(context, purchase, DateTime.UtcNow.AddDays(-daysAgo));
            pairSet.Add((userId, bookId));
            daysAgo -= 2;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureFavoritesAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Guid> users,
        IReadOnlyDictionary<Guid, Guid> books,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new UserBookSeed(DemoSeedData.MainBuyerPhoneNumber, DemoSeedData.Books.Dune),
            new UserBookSeed(DemoSeedData.MainBuyerPhoneNumber, DemoSeedData.Books.CleanCode),
            new UserBookSeed(DemoSeedData.MainBuyerPhoneNumber, DemoSeedData.Books.Granada),
            new UserBookSeed(DemoSeedData.SellerPhoneNumber, DemoSeedData.Books.Dune),
            new UserBookSeed(DemoSeedData.SellerPhoneNumber, DemoSeedData.Books.Muqaddimah),
            new UserBookSeed(DemoSeedData.CheckoutBuyerPhoneNumber, DemoSeedData.Books.AtomicHabits),
            new UserBookSeed(DemoSeedData.CheckoutBuyerPhoneNumber, DemoSeedData.Books.CleanCode),
            new UserBookSeed(DemoSeedData.ReporterOnePhoneNumber, DemoSeedData.Books.Granada),
            new UserBookSeed(DemoSeedData.ReporterTwoPhoneNumber, DemoSeedData.Books.Dune),
            new UserBookSeed(DemoSeedData.ReporterThreePhoneNumber, DemoSeedData.Books.LePetitPrince),
        };

        var existing = await context.FavoriteBooks
            .Where(favorite => !favorite.IsDeleted)
            .Select(favorite => new { favorite.UserId, favorite.BookId })
            .ToListAsync(cancellationToken);
        var pairSet = existing.Select(pair => (pair.UserId, pair.BookId)).ToHashSet();

        foreach (var definition in definitions)
        {
            var pair = (users[definition.PhoneNumber], books[definition.BookId]);
            if (pairSet.Add(pair))
            {
                await context.FavoriteBooks.AddAsync(
                    FavoriteBookAggregate.Create(pair.Item1, pair.Item2),
                    cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureRatingsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Guid> users,
        IReadOnlyDictionary<Guid, Guid> books,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new UserBookSeed(DemoSeedData.MainBuyerPhoneNumber, DemoSeedData.Books.Dune, 5),
            new UserBookSeed(DemoSeedData.SellerPhoneNumber, DemoSeedData.Books.Dune, 4),
            new UserBookSeed(DemoSeedData.CheckoutBuyerPhoneNumber, DemoSeedData.Books.Dune, 5),
            new UserBookSeed(DemoSeedData.ReporterTwoPhoneNumber, DemoSeedData.Books.Dune, 4),
            new UserBookSeed(DemoSeedData.ReporterThreePhoneNumber, DemoSeedData.Books.Dune, 5),
            new UserBookSeed(DemoSeedData.MainBuyerPhoneNumber, DemoSeedData.Books.CleanCode, 5),
            new UserBookSeed(DemoSeedData.SellerPhoneNumber, DemoSeedData.Books.CleanCode, 5),
            new UserBookSeed(DemoSeedData.ReporterOnePhoneNumber, DemoSeedData.Books.CleanCode, 4),
            new UserBookSeed(DemoSeedData.ReporterThreePhoneNumber, DemoSeedData.Books.CleanCode, 4),
            new UserBookSeed(DemoSeedData.MainBuyerPhoneNumber, DemoSeedData.Books.Granada, 4),
            new UserBookSeed(DemoSeedData.ReporterTwoPhoneNumber, DemoSeedData.Books.Granada, 5),
            new UserBookSeed(DemoSeedData.ReporterOnePhoneNumber, DemoSeedData.Books.PragmaticProgrammer, 5),
        };

        var existing = await context.BookRatings
            .Select(rating => new { rating.UserId, rating.BookId })
            .ToListAsync(cancellationToken);
        var pairSet = existing.Select(pair => (pair.UserId, pair.BookId)).ToHashSet();

        foreach (var definition in definitions)
        {
            var pair = (users[definition.PhoneNumber], books[definition.BookId]);
            if (pairSet.Add(pair))
            {
                await context.BookRatings.AddAsync(
                    BookRatingAggregate.Create(pair.Item1, pair.Item2, definition.Rating!.Value),
                    cancellationToken);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCommentsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Guid> users,
        IReadOnlyDictionary<Guid, Guid> books,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new CommentSeed(DemoSeedData.MainBuyerPhoneNumber, DemoSeedData.Books.Dune,
                "The world-building is rich, but the ecological ideas are what stayed with me.", 28),
            new CommentSeed(DemoSeedData.SellerPhoneNumber, DemoSeedData.Books.Dune,
                "A slow opening that rewards patience with an unforgettable political story.", 24),
            new CommentSeed(DemoSeedData.CheckoutBuyerPhoneNumber, DemoSeedData.Books.Dune,
                "Excellent digital edition and a great discussion book.", 18),
            new CommentSeed(DemoSeedData.ReporterTwoPhoneNumber, DemoSeedData.Books.Dune,
                "The characters and competing loyalties made this a five-star reading experience.", 12),
            new CommentSeed(DemoSeedData.ReporterThreePhoneNumber, DemoSeedData.Books.Dune,
                "I finally understand why this novel influenced so much modern science fiction.", 6),
            new CommentSeed(DemoSeedData.MainBuyerPhoneNumber, DemoSeedData.Books.CleanCode,
                "Concrete examples that made our team discuss naming and function boundaries.", 13),
            new CommentSeed(DemoSeedData.SellerPhoneNumber, DemoSeedData.Books.CleanCode,
                "Useful as a conversation starter even when you disagree with a rule.", 11),
            new CommentSeed(DemoSeedData.ReporterOnePhoneNumber, DemoSeedData.Books.CleanCode,
                "The refactoring examples are practical and easy to demonstrate in a review.", 8),
            new CommentSeed(DemoSeedData.ReporterThreePhoneNumber, DemoSeedData.Books.CleanCode,
                "A solid reference for junior developers learning maintainable code.", 4),
            new CommentSeed(DemoSeedData.MainBuyerPhoneNumber, DemoSeedData.Books.Granada,
                "سرد مؤثر يجعل التاريخ قريباً من القارئ ويمنح الشخصيات ذاكرة حقيقية.", 10),
            new CommentSeed(DemoSeedData.ReporterTwoPhoneNumber, DemoSeedData.Books.Granada,
                "من أجمل الروايات التاريخية التي قرأتها، والجزء الأخير شديد التأثير.", 3),
            new CommentSeed(DemoSeedData.CheckoutBuyerPhoneNumber, DemoSeedData.Books.AtomicHabits,
                "The focus on systems over goals is simple and immediately actionable.", 9),
        };

        var userIds = definitions
            .Select(definition => users[definition.PhoneNumber])
            .Distinct()
            .ToArray();
        var bookIds = definitions
            .Select(definition => books[definition.BookId])
            .Distinct()
            .ToArray();
        var existingPairs = await context.Comments
            .Where(comment =>
                userIds.Contains(comment.UserId) &&
                bookIds.Contains(comment.BookId))
            .Select(comment => new { comment.UserId, comment.BookId })
            .ToListAsync(cancellationToken);
        var pairSet = existingPairs
            .Select(pair => (pair.UserId, pair.BookId))
            .ToHashSet();

        foreach (var definition in definitions)
        {
            var userId = users[definition.PhoneNumber];
            var bookId = books[definition.BookId];
            if (!pairSet.Add((userId, bookId)))
            {
                continue;
            }

            var comment = CommentAggregate.Create(userId, bookId, definition.Content);
            await context.Comments.AddAsync(comment, cancellationToken);
            SetCreationTime(context, comment, DateTime.UtcNow.AddDays(-definition.DaysAgo));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureReportsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<string, Guid> users,
        IReadOnlyDictionary<Guid, Guid> books,
        CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            new ReportSeed(DemoSeedData.ReporterOnePhoneNumber, DemoSeedData.Books.ModerationFlagged,
                BookReportReason.IncorrectInformation,
                "The edition metadata does not match the displayed cover.",
                BookReportStatus.Pending),
            new ReportSeed(DemoSeedData.ReporterOnePhoneNumber, DemoSeedData.Books.ModerationHidden,
                BookReportReason.PoorQuality,
                "Several pages in the scan are unreadable.",
                BookReportStatus.Pending),
            new ReportSeed(DemoSeedData.ReporterTwoPhoneNumber, DemoSeedData.Books.ModerationHidden,
                BookReportReason.PoorQuality,
                "The document has missing pages and severe scan artifacts.",
                BookReportStatus.InReview),
            new ReportSeed(DemoSeedData.ReporterThreePhoneNumber, DemoSeedData.Books.ModerationHidden,
                BookReportReason.CopyrightViolation,
                "The uploaded edition appears to lack publication permission.",
                BookReportStatus.Resolved),
            new ReportSeed(DemoSeedData.SellerPhoneNumber, DemoSeedData.Books.CleanCode,
                BookReportReason.IncorrectInformation,
                "Demo report intentionally dismissed after metadata verification.",
                BookReportStatus.Rejected),
            new ReportSeed(DemoSeedData.CheckoutBuyerPhoneNumber, DemoSeedData.Books.CleanCode,
                BookReportReason.PoorQuality,
                "Demo report intentionally dismissed after the file was checked.",
                BookReportStatus.Rejected),
            new ReportSeed(DemoSeedData.ReporterTwoPhoneNumber, DemoSeedData.Books.CleanCode,
                BookReportReason.Other,
                "Interview demo of a closed report that did not require catalog action.",
                BookReportStatus.Rejected),
        };

        var adminId = users[DemoSeedData.AdminPhoneNumber];
        var addedByBook = new HashSet<Guid>();

        foreach (var definition in definitions)
        {
            var userId = users[definition.PhoneNumber];
            var bookId = books[definition.BookId];
            var exists = await context.BookReports.AnyAsync(
                report =>
                    !report.IsDeleted &&
                    report.UserId == userId &&
                    report.BookId == bookId,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            var report = BookReportAggregate.Create(
                userId,
                bookId,
                definition.Reason,
                definition.Details);

            if (definition.Status != BookReportStatus.Pending)
            {
                report.Review(
                    definition.Status,
                    adminId,
                    definition.Status == BookReportStatus.Rejected
                        ? "Reviewed for the interview dataset; no violation was confirmed."
                        : "Reviewed for the interview dataset; catalog action is recorded.");
            }

            await context.BookReports.AddAsync(report, cancellationToken);
            addedByBook.Add(bookId);
        }

        if (addedByBook.Contains(books[DemoSeedData.Books.ModerationFlagged]))
        {
            var flaggedBook = await context.Books
                .IgnoreQueryFilters()
                .SingleAsync(
                    book => book.Id == books[DemoSeedData.Books.ModerationFlagged],
                    cancellationToken);
            flaggedBook.Flag("Reported by one distinct demo reader; still publicly visible.");
        }

        if (addedByBook.Contains(books[DemoSeedData.Books.ModerationHidden]))
        {
            var hiddenBook = await context.Books
                .IgnoreQueryFilters()
                .SingleAsync(
                    book => book.Id == books[DemoSeedData.Books.ModerationHidden],
                    cancellationToken);
            hiddenBook.HideForReview(
                "Three distinct demo readers reported this edition.",
                adminId);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, Guid>> GetUsersAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var phoneNumbers = DemoSeedData.Users
            .Select(user => user.PhoneNumber)
            .ToArray();
        var result = await context.UsersProfiles
            .Where(user => phoneNumbers.Contains(user.PhoneNumber))
            .ToDictionaryAsync(
                user => user.PhoneNumber,
                user => user.Id,
                StringComparer.Ordinal,
                cancellationToken);

        if (result.Count != phoneNumbers.Length)
        {
            throw new InvalidOperationException("One or more demo engagement users are missing.");
        }

        return result;
    }

    private static async Task<Dictionary<Guid, ListingAggregate>> GetListingsAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var listingIds = new[]
        {
            DemoSeedData.Listings.GranadaLibrary,
            DemoSeedData.Listings.CleanCodeLibraryOne,
            DemoSeedData.Listings.DuneDigital,
            DemoSeedData.Listings.AtomicHabitsOutOfStock,
        };

        var listings = await context.Listings
            .Where(listing => listingIds.Contains(listing.Id))
            .ToListAsync(cancellationToken);

        if (listings.Count != listingIds.Length)
        {
            throw new InvalidOperationException("One or more demo engagement listings are missing.");
        }

        return listings.ToDictionary(listing => listing.Id);
    }

    private static void SetCreationTime(
        ApplicationDbContext context,
        AuditableEntity entity,
        DateTime creationTime)
    {
        context.Entry(entity)
            .Property(nameof(AuditableEntity.CreationTime))
            .CurrentValue = creationTime;
    }
}
