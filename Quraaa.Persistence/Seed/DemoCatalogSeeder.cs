using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Author;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed;

public static class DemoCatalogSeeder
{
    private const string CoverOne =
        "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=600&q=85";
    private const string CoverTwo =
        "https://images.unsplash.com/photo-1543002588-bfa74002ed7e?w=600&q=85";
    private const string UserListingCover =
        "https://images.unsplash.com/photo-1589998059171-988d887df646?w=600&q=85";

    private sealed record AuthorSeed(
        Guid Id,
        string Name,
        string Bio,
        string PhotoUrl,
        DateTime? BirthDate,
        bool IsInactive = false);

    private sealed record BookSeed(
        Guid Id,
        string Isbn,
        string Title,
        Guid AuthorSeedId,
        string Description,
        string CoverImageUrl,
        Language Language,
        Guid CategoryId,
        string? CanonicalPdfUrl = null);

    public static async Task<IReadOnlyDictionary<Guid, Guid>> SeedAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var moderatorId = await GetUserIdAsync(
            context,
            DemoSeedData.SuperAdminPhoneNumber,
            cancellationToken);

        var authorIdMap = await EnsureAuthorsAsync(
            context,
            moderatorId,
            cancellationToken);

        var bookIdMap = await EnsureBooksAsync(
            context,
            authorIdMap,
            moderatorId,
            cancellationToken);

        await EnsureListingsAsync(
            context,
            bookIdMap,
            cancellationToken);

        return bookIdMap;
    }

    private static async Task<Dictionary<Guid, Guid>> EnsureAuthorsAsync(
        ApplicationDbContext context,
        Guid moderatorId,
        CancellationToken cancellationToken)
    {
        var definitions = GetAuthors();
        var ids = definitions.Select(definition => definition.Id).ToArray();
        var names = definitions.Select(definition => definition.Name).ToArray();

        var existing = await context.Authors
            .Where(author => ids.Contains(author.Id) || names.Contains(author.Name))
            .OrderBy(author => author.CreationTime)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, Guid>();

        foreach (var definition in definitions)
        {
            var byId = existing.FirstOrDefault(author => author.Id == definition.Id);
            if (byId is not null &&
                !string.Equals(byId.Name, definition.Name, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Demo author id {definition.Id} belongs to {byId.Name}.");
            }

            var author = byId ?? existing.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, definition.Name, StringComparison.Ordinal));

            if (author is null)
            {
                author = new AuthorAggregate(
                    definition.Id,
                    definition.Name,
                    definition.Bio,
                    definition.PhotoUrl,
                    definition.BirthDate);

                if (definition.IsInactive)
                {
                    author.Delete(moderatorId);
                }

                await context.Authors.AddAsync(author, cancellationToken);
                existing.Add(author);
            }
            else if (string.IsNullOrWhiteSpace(author.Bio) && !author.IsDeleted)
            {
                author.UpdateDetails(
                    definition.Name,
                    definition.Bio,
                    definition.PhotoUrl,
                    definition.BirthDate,
                    moderatorId);
            }

            result[definition.Id] = author.Id;
        }

        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static async Task<Dictionary<Guid, Guid>> EnsureBooksAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<Guid, Guid> authorIdMap,
        Guid moderatorId,
        CancellationToken cancellationToken)
    {
        var definitions = GetBooks();
        var ids = definitions.Select(definition => definition.Id).ToArray();
        var isbns = definitions.Select(definition => definition.Isbn).ToArray();
        var relevantAuthorIds = definitions
            .Select(definition => authorIdMap[definition.AuthorSeedId])
            .Distinct()
            .ToArray();

        var existing = await context.Books
            .IgnoreQueryFilters()
            .Where(book =>
                ids.Contains(book.Id) ||
                (book.Isbn != null && isbns.Contains(book.Isbn)) ||
                (book.AuthorId.HasValue && relevantAuthorIds.Contains(book.AuthorId.Value)))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, Guid>();

        foreach (var definition in definitions)
        {
            var expectedAuthorId = authorIdMap[definition.AuthorSeedId];
            var byId = existing.FirstOrDefault(book => book.Id == definition.Id);
            var byIsbn = existing.FirstOrDefault(book => book.Isbn == definition.Isbn);
            var byNaturalKey = existing.FirstOrDefault(book =>
                book.AuthorId == expectedAuthorId &&
                book.Language == definition.Language &&
                string.Equals(
                    book.Title,
                    definition.Title,
                    StringComparison.OrdinalIgnoreCase));

            if (byId is not null &&
                (byId.Isbn != definition.Isbn ||
                 byId.AuthorId != expectedAuthorId ||
                 byId.Language != definition.Language ||
                 !string.Equals(
                     byId.Title,
                     definition.Title,
                     StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Demo book id {definition.Id} belongs to another catalog record.");
            }

            if (byIsbn is not null &&
                (byIsbn.AuthorId != expectedAuthorId ||
                 byIsbn.Language != definition.Language ||
                 !string.Equals(
                     byIsbn.Title,
                     definition.Title,
                     StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Demo ISBN {definition.Isbn} belongs to another catalog record.");
            }

            var matchedIds = new[] { byId?.Id, byIsbn?.Id, byNaturalKey?.Id }
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToArray();
            if (matchedIds.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Demo book '{definition.Title}' matches multiple catalog records.");
            }

            var book = byId ?? byIsbn ?? byNaturalKey;

            if (book is null)
            {
                var initialDescription = definition.Id == DemoSeedData.Books.CleanCode
                    ? "Initial catalog draft retained to demonstrate book version history."
                    : definition.Description;

                book = new BookAggregate(
                    definition.Id,
                    definition.Title,
                    expectedAuthorId,
                    initialDescription,
                    definition.CoverImageUrl,
                    definition.Language,
                    definition.CategoryId,
                    definition.Isbn,
                    definition.CanonicalPdfUrl);

                await context.Books.AddAsync(book, cancellationToken);
                existing.Add(book);
            }

            result[definition.Id] = book.Id;
        }

        await context.SaveChangesAsync(cancellationToken);
        await EnsureCurrentBookVersionsAsync(context, result.Values, cancellationToken);

        var cleanCodeId = result[DemoSeedData.Books.CleanCode];
        var cleanCode = existing.Single(book => book.Id == cleanCodeId);
        var cleanCodeDefinition = definitions.Single(
            definition => definition.Id == DemoSeedData.Books.CleanCode);

        if (cleanCode.CurrentVersionNumber == 1 &&
            cleanCode.Description.StartsWith("Initial catalog draft", StringComparison.Ordinal))
        {
            cleanCode.ApplyDetails(
                cleanCodeDefinition.Title,
                authorIdMap[cleanCodeDefinition.AuthorSeedId],
                cleanCodeDefinition.Description,
                cleanCodeDefinition.CoverImageUrl,
                cleanCodeDefinition.CategoryId,
                cleanCodeDefinition.Language,
                cleanCodeDefinition.Isbn,
                moderatorId);

            await context.SaveChangesAsync(cancellationToken);
            await EnsureCurrentBookVersionsAsync(
                context,
                [cleanCode.Id],
                cancellationToken);
        }

        return result;
    }

    private static async Task EnsureCurrentBookVersionsAsync(
        ApplicationDbContext context,
        IEnumerable<Guid> bookIds,
        CancellationToken cancellationToken)
    {
        var ids = bookIds.Distinct().ToArray();
        var books = await context.Books
            .IgnoreQueryFilters()
            .Where(book => ids.Contains(book.Id))
            .ToListAsync(cancellationToken);

        var existingKeys = await context.BookVersions
            .Where(version => ids.Contains(version.BookId))
            .Select(version => new { version.BookId, version.VersionNumber })
            .ToListAsync(cancellationToken);

        var keySet = existingKeys
            .Select(key => (key.BookId, key.VersionNumber))
            .ToHashSet();

        var missing = books
            .Where(book => !keySet.Contains((book.Id, book.CurrentVersionNumber)))
            .Select(book => BookVersion.Capture(
                book,
                book.CurrentVersionNumber == 1
                    ? BookVersionReason.Created
                    : BookVersionReason.Edited,
                book.LastModifiedBy))
            .ToArray();

        if (missing.Length == 0)
        {
            return;
        }

        await context.BookVersions.AddRangeAsync(missing, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureListingsAsync(
        ApplicationDbContext context,
        IReadOnlyDictionary<Guid, Guid> bookIdMap,
        CancellationToken cancellationToken)
    {
        var libraryEmails = new[]
        {
            "info.lib1@quraaa.com",
            "info.lib2@quraaa.com",
            "info.lib3@quraaa.com",
        };

        var libraries = await context.Libraries
            .Where(library =>
                !library.IsDeleted &&
                library.ApprovalStatus == LibraryApprovalStatus.Approved &&
                libraryEmails.Contains(library.Email))
            .ToDictionaryAsync(
                library => library.Email,
                library => library.Id,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        if (libraries.Count < libraryEmails.Length)
        {
            return;
        }

        var sellerUserId = await GetUserIdAsync(
            context,
            DemoSeedData.SellerPhoneNumber,
            cancellationToken);

        var existingById = await context.Listings
            .Where(listing => new[]
            {
                DemoSeedData.Listings.GranadaLibrary,
                DemoSeedData.Listings.CleanCodeLibraryOne,
                DemoSeedData.Listings.CleanCodeLibraryTwo,
                DemoSeedData.Listings.DuneDigital,
                DemoSeedData.Listings.DunePhysical,
                DemoSeedData.Listings.AtomicHabitsOutOfStock,
                DemoSeedData.Listings.MuqaddimahRemoved,
                DemoSeedData.Listings.UtopiaUser,
                DemoSeedData.Listings.PragmaticProgrammerUser,
                DemoSeedData.Listings.FlaggedBookLibrary,
                DemoSeedData.Listings.HiddenBookLibrary,
                DemoSeedData.Listings.HiddenBookLibraryTwo,
                DemoSeedData.Listings.LePetitPrinceLibrary,
            }.Contains(listing.Id))
            .ToDictionaryAsync(listing => listing.Id, cancellationToken);

        var additions = new List<ListingAggregate>();

        AddLibraryPhysical(
            additions, existingById, DemoSeedData.Listings.GranadaLibrary,
            bookIdMap[DemoSeedData.Books.Granada], libraries["info.lib1@quraaa.com"],
            18.50m, BookCondition.LikeNew, 9);
        AddLibraryPhysical(
            additions, existingById, DemoSeedData.Listings.CleanCodeLibraryOne,
            bookIdMap[DemoSeedData.Books.CleanCode], libraries["info.lib1@quraaa.com"],
            34.90m, BookCondition.New, 12);
        AddLibraryPhysical(
            additions, existingById, DemoSeedData.Listings.CleanCodeLibraryTwo,
            bookIdMap[DemoSeedData.Books.CleanCode], libraries["info.lib2@quraaa.com"],
            31.50m, BookCondition.Good, 7);
        AddLibraryDigital(
            additions, existingById, DemoSeedData.Listings.DuneDigital,
            bookIdMap[DemoSeedData.Books.Dune], libraries["info.lib1@quraaa.com"],
            8.99m, "books/book1.pdf");
        AddLibraryPhysical(
            additions, existingById, DemoSeedData.Listings.DunePhysical,
            bookIdMap[DemoSeedData.Books.Dune], libraries["info.lib3@quraaa.com"],
            24.75m, BookCondition.Acceptable, 5);

        if (existingById.TryGetValue(
                DemoSeedData.Listings.AtomicHabitsOutOfStock,
                out var existingAtomicHabits))
        {
            ValidateListing(
                existingAtomicHabits,
                bookIdMap[DemoSeedData.Books.AtomicHabits],
                SellerType.Library,
                ListingFormat.Physical,
                libraries["info.lib2@quraaa.com"],
                null);
        }
        else
        {
            var listing = ListingAggregate.CreateForLibrary(
                DemoSeedData.Listings.AtomicHabitsOutOfStock,
                bookIdMap[DemoSeedData.Books.AtomicHabits],
                libraries["info.lib2@quraaa.com"],
                21.00m,
                BookCondition.New,
                3);
            listing.UpdateStock(0, libraries["info.lib2@quraaa.com"]);
            listing.ClearDomainEvents();
            additions.Add(listing);
        }

        if (existingById.TryGetValue(
                DemoSeedData.Listings.MuqaddimahRemoved,
                out var existingMuqaddimah))
        {
            ValidateListing(
                existingMuqaddimah,
                bookIdMap[DemoSeedData.Books.Muqaddimah],
                SellerType.Library,
                ListingFormat.Physical,
                libraries["info.lib3@quraaa.com"],
                null);
        }
        else
        {
            var listing = ListingAggregate.CreateForLibrary(
                DemoSeedData.Listings.MuqaddimahRemoved,
                bookIdMap[DemoSeedData.Books.Muqaddimah],
                libraries["info.lib3@quraaa.com"],
                42.00m,
                BookCondition.Good,
                4);
            listing.Remove(libraries["info.lib3@quraaa.com"]);
            listing.ClearDomainEvents();
            additions.Add(listing);
        }

        AddUserPhysical(
            additions, existingById, DemoSeedData.Listings.UtopiaUser,
            bookIdMap[DemoSeedData.Books.Utopia], sellerUserId,
            12.50m, BookCondition.Good);
        AddUserPhysical(
            additions, existingById, DemoSeedData.Listings.PragmaticProgrammerUser,
            bookIdMap[DemoSeedData.Books.PragmaticProgrammer], sellerUserId,
            29.00m, BookCondition.LikeNew);
        AddLibraryPhysical(
            additions, existingById, DemoSeedData.Listings.FlaggedBookLibrary,
            bookIdMap[DemoSeedData.Books.ModerationFlagged], libraries["info.lib1@quraaa.com"],
            16.00m, BookCondition.Good, 6);
        AddLibraryPhysical(
            additions, existingById, DemoSeedData.Listings.HiddenBookLibrary,
            bookIdMap[DemoSeedData.Books.ModerationHidden], libraries["info.lib1@quraaa.com"],
            15.00m, BookCondition.Acceptable, 6);
        AddLibraryPhysical(
            additions, existingById, DemoSeedData.Listings.HiddenBookLibraryTwo,
            bookIdMap[DemoSeedData.Books.ModerationHidden], libraries["info.lib2@quraaa.com"],
            14.50m, BookCondition.Good, 4);
        AddLibraryPhysical(
            additions, existingById, DemoSeedData.Listings.LePetitPrinceLibrary,
            bookIdMap[DemoSeedData.Books.LePetitPrince], libraries["info.lib3@quraaa.com"],
            19.95m, BookCondition.New, 8);

        if (additions.Count == 0)
        {
            return;
        }

        await context.Listings.AddRangeAsync(additions, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void AddLibraryPhysical(
        ICollection<ListingAggregate> additions,
        IReadOnlyDictionary<Guid, ListingAggregate> existingById,
        Guid id,
        Guid bookId,
        Guid libraryId,
        decimal price,
        BookCondition condition,
        int stock)
    {
        if (existingById.TryGetValue(id, out var existing))
        {
            ValidateListing(
                existing,
                bookId,
                SellerType.Library,
                ListingFormat.Physical,
                libraryId,
                null);
            return;
        }

        var listing = ListingAggregate.CreateForLibrary(
            id, bookId, libraryId, price, condition, stock);
        listing.ClearDomainEvents();
        additions.Add(listing);
    }

    private static void AddLibraryDigital(
        ICollection<ListingAggregate> additions,
        IReadOnlyDictionary<Guid, ListingAggregate> existingById,
        Guid id,
        Guid bookId,
        Guid libraryId,
        decimal price,
        string digitalAssetUrl)
    {
        if (existingById.TryGetValue(id, out var existing))
        {
            ValidateListing(
                existing,
                bookId,
                SellerType.Library,
                ListingFormat.Digital,
                libraryId,
                null);
            return;
        }

        var listing = ListingAggregate.CreateDigitalForLibrary(
            id, bookId, libraryId, price, digitalAssetUrl);
        listing.ClearDomainEvents();
        additions.Add(listing);
    }

    private static void AddUserPhysical(
        ICollection<ListingAggregate> additions,
        IReadOnlyDictionary<Guid, ListingAggregate> existingById,
        Guid id,
        Guid bookId,
        Guid userId,
        decimal price,
        BookCondition condition)
    {
        if (existingById.TryGetValue(id, out var existing))
        {
            ValidateListing(
                existing,
                bookId,
                SellerType.User,
                ListingFormat.Physical,
                null,
                userId);
            return;
        }

        additions.Add(ListingAggregate.CreateForUser(
            id,
            bookId,
            userId,
            ListingFormat.Physical,
            price,
            condition,
            customCoverImageUrl: UserListingCover));
    }

    private static void ValidateListing(
        ListingAggregate listing,
        Guid expectedBookId,
        SellerType expectedSellerType,
        ListingFormat expectedFormat,
        Guid? expectedLibraryId,
        Guid? expectedUserId)
    {
        if (listing.BookId != expectedBookId ||
            listing.SellerType != expectedSellerType ||
            listing.Format != expectedFormat ||
            listing.LibraryId != expectedLibraryId ||
            listing.UserId != expectedUserId)
        {
            throw new InvalidOperationException(
                $"Demo listing id {listing.Id} belongs to another marketplace record.");
        }
    }

    private static async Task<Guid> GetUserIdAsync(
        ApplicationDbContext context,
        string phoneNumber,
        CancellationToken cancellationToken)
    {
        var userId = await context.UsersProfiles
            .Where(user => user.PhoneNumber == phoneNumber)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return userId ?? throw new InvalidOperationException(
            $"Demo user {phoneNumber} must be seeded before catalog data.");
    }

    private static IReadOnlyList<AuthorSeed> GetAuthors() =>
    [
        new(DemoSeedData.Authors.RadwaAshour, "Radwa Ashour",
            "Egyptian novelist and scholar known for historically grounded fiction and the Granada trilogy.",
            "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=400&q=80",
            new DateTime(1946, 5, 26, 0, 0, 0, DateTimeKind.Utc)),
        new(DemoSeedData.Authors.AhmedKhaledTowfik, "Ahmed Khaled Towfik",
            "Egyptian physician and influential Arabic speculative-fiction writer.",
            "https://images.unsplash.com/photo-1500648767791-00dcc994a43e?w=400&q=80",
            new DateTime(1962, 6, 10, 0, 0, 0, DateTimeKind.Utc)),
        new(DemoSeedData.Authors.IbnKhaldun, "Ibn Khaldun",
            "Historian and thinker whose Muqaddimah pioneered systematic approaches to society and history.",
            "https://images.unsplash.com/photo-1560250097-0b93528c311a?w=400&q=80",
            new DateTime(1332, 5, 27, 0, 0, 0, DateTimeKind.Utc)),
        new(DemoSeedData.Authors.RobertMartin, "Robert C. Martin",
            "Software engineer and author focused on maintainable code and professional practice.",
            "https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=400&q=80",
            new DateTime(1952, 12, 5, 0, 0, 0, DateTimeKind.Utc)),
        new(DemoSeedData.Authors.FrankHerbert, "Frank Herbert",
            "American science-fiction author best known for the ecological and political world of Dune.",
            "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&q=80",
            new DateTime(1920, 10, 8, 0, 0, 0, DateTimeKind.Utc)),
        new(DemoSeedData.Authors.JamesClear, "James Clear",
            "Writer on habits, decision-making, and continuous improvement.",
            "https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?w=400&q=80",
            new DateTime(1986, 1, 22, 0, 0, 0, DateTimeKind.Utc)),
        new(DemoSeedData.Authors.AndrewHunt, "Andrew Hunt",
            "Software developer, publisher, and co-author of The Pragmatic Programmer.",
            "https://images.unsplash.com/photo-1507591064344-4c6ce005b128?w=400&q=80",
            new DateTime(1964, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
        new(DemoSeedData.Authors.DemoInactive, "Archived Demo Author",
            "An intentionally deactivated author used to demonstrate admin reactivation.",
            "https://images.unsplash.com/photo-1568602471122-7832951cc4c5?w=400&q=80",
            null,
            IsInactive: true),
        new(DemoSeedData.Authors.AntoineDeSaintExupery, "Antoine de Saint-Exupery",
            "French writer and aviator whose stories combine adventure, responsibility, and human connection.",
            "https://images.unsplash.com/photo-1564564321837-a57b7070ac4f?w=400&q=80",
            new DateTime(1900, 6, 29, 0, 0, 0, DateTimeKind.Utc)),
    ];

    private static IReadOnlyList<BookSeed> GetBooks() =>
    [
        new(DemoSeedData.Books.Granada, "9780000001001", "ثلاثية غرناطة",
            DemoSeedData.Authors.RadwaAshour,
            "رواية تاريخية تستكشف الذاكرة والهوية والمقاومة عبر أجيال من عائلة أندلسية.",
            CoverOne, Language.Arabic, CategoryIds.Novels),
        new(DemoSeedData.Books.Utopia, "9780000001002", "يوتوبيا",
            DemoSeedData.Authors.AhmedKhaledTowfik,
            "رواية ديستوبية عربية تناقش الانقسام الاجتماعي والإنساني في مستقبل قريب.",
            CoverOne, Language.Arabic, CategoryIds.Novels),
        new(DemoSeedData.Books.Muqaddimah, "9780000001003", "مقدمة ابن خلدون",
            DemoSeedData.Authors.IbnKhaldun,
            "عمل تأسيسي في فهم العمران البشري وتحولات الدول والمجتمعات.",
            CoverOne, Language.Arabic, CategoryIds.History),
        new(DemoSeedData.Books.CleanCode, "9780000001004", "Clean Code",
            DemoSeedData.Authors.RobertMartin,
            "A practical guide to readable, maintainable software and disciplined engineering habits.",
            CoverTwo, Language.English, CategoryIds.Technology),
        new(DemoSeedData.Books.Dune, "9780000001005", "Dune",
            DemoSeedData.Authors.FrankHerbert,
            "A landmark science-fiction novel about ecology, power, belief, and survival on Arrakis.",
            CoverTwo, Language.English, CategoryIds.Novels,
            CanonicalPdfUrl: "books/book1.pdf"),
        new(DemoSeedData.Books.AtomicHabits, "9780000001006", "Atomic Habits",
            DemoSeedData.Authors.JamesClear,
            "A systems-oriented approach to building good habits and improving through small changes.",
            CoverTwo, Language.English, CategoryIds.Education),
        new(DemoSeedData.Books.PragmaticProgrammer, "9780000001007", "The Pragmatic Programmer",
            DemoSeedData.Authors.AndrewHunt,
            "Practical lessons for adaptable software design, debugging, teamwork, and career growth.",
            CoverTwo, Language.English, CategoryIds.Technology),
        new(DemoSeedData.Books.ModerationFlagged, "9780000001008", "The Unverified Edition",
            DemoSeedData.Authors.AndrewHunt,
            "A visible catalog record with one open report, used to demonstrate first-level moderation.",
            CoverTwo, Language.English, CategoryIds.Technology),
        new(DemoSeedData.Books.ModerationHidden, "9780000001009", "Damaged Archive Scan",
            DemoSeedData.Authors.RadwaAshour,
            "A catalog record withheld after multiple quality reports, visible only to moderation workflows.",
            CoverOne, Language.Arabic, CategoryIds.Literature),
        new(DemoSeedData.Books.LePetitPrince, "9780000001010", "Le Petit Prince",
            DemoSeedData.Authors.AntoineDeSaintExupery,
            "Un conte poétique sur l'amitié, la responsabilité et ce qui donne du sens à la vie.",
            CoverTwo, Language.French, CategoryIds.Literature),
    ];
}
