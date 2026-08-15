using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly ApplicationDbContext _context;

        public BookRepository(ApplicationDbContext context) => _context = context;

        public async Task<BookAggregate?> FindByIsbnAsync(
            string isbn, CancellationToken cancellationToken = default) =>
            await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Isbn == isbn, cancellationToken);

        public async Task<BookAggregate?> GetByIdAsync(
            Guid id, CancellationToken cancellationToken = default) =>
            await _context.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        public async Task<BookAggregate?> FindByTitleAuthorLanguageAsync(
            string title, string author, Language language,
            CancellationToken cancellationToken = default) =>
            await _context.Books
                .AsNoTracking()
                .GroupJoin(
                    _context.Authors.AsNoTracking(),
                    b => b.AuthorId,
                    a => a.Id,
                    (b, authors) => new { Book = b, Authors = authors })
                .SelectMany(
                    x => x.Authors.DefaultIfEmpty(),
                    (x, a) => new { x.Book, AuthorName = a != null ? a.Name : null })
                .Where(x =>
                    EF.Functions.ILike(x.Book.Title, title) &&
                    EF.Functions.ILike(x.AuthorName!, author) &&
                    x.Book.Language == language)
                .Select(x => x.Book)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IReadOnlyList<(string Title, string? Author, Language Language)>>
            FindExistingCandidatesAsync(
                IReadOnlyList<string> normalizedTitles,
                CancellationToken cancellationToken = default)
        {
            if (normalizedTitles.Count == 0)
                return [];

            // EF Core + Npgsql translates string.ToLower() → lower() in PostgreSQL.
            // normalizedTitles.Contains(...) → lower("Title") IN ('t1', 't2', ...)
            // Author is resolved via a left join since AuthorId is optional.
            var matches = await _context.Books
                .AsNoTracking()
                .Where(b => normalizedTitles.Contains(b.Title.ToLower()))
                .GroupJoin(
                    _context.Authors.AsNoTracking(),
                    b => b.AuthorId,
                    a => a.Id,
                    (b, authors) => new { Book = b, Authors = authors })
                .SelectMany(
                    x => x.Authors.DefaultIfEmpty(),
                    (x, a) => new { x.Book.Title, AuthorName = a != null ? a.Name : null, x.Book.Language })
                .ToListAsync(cancellationToken);

            return matches
                .Select(m => (m.Title, m.AuthorName, m.Language))
                .ToList();
        }

        public async Task AddAsync(
            BookAggregate book, CancellationToken cancellationToken = default) =>
            await _context.Books.AddAsync(book, cancellationToken);

        public async Task BulkInsertAsync(
            IReadOnlyList<BookAggregate> books,
            CancellationToken cancellationToken = default)
        {
            await _context.Books.AddRangeAsync(books, cancellationToken);

            try
            {
                // EF Core + Npgsql batches multiple inserts into a single round-trip
                // using PostgreSQL multi-row INSERT syntax (up to the configured batch size).
                // For 1 000+ rows, swap this for EFCore.BulkExtensions:
                //   await _context.BulkInsertAsync(books, cancellationToken: cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Detach pending entities so the DbContext remains usable after the failure.
                foreach (var entry in _context.ChangeTracker.Entries<BookAggregate>().ToList())
                    entry.State = EntityState.Detached;

                throw new ConflictException(
                    "One or more books already exist with the same Title, Author, and Language combination.");
            }
        }

        public async Task<HashSet<string>> FilterReferencedCanonicalAssetPathsAsync(
            IReadOnlyCollection<string> storedReferences,
            CancellationToken cancellationToken = default)
        {
            if (storedReferences.Count == 0)
                return new HashSet<string>(StringComparer.Ordinal);

            var references = storedReferences.ToList();
            var matches = await _context.Books
                .AsNoTracking()
                .Where(book =>
                    (book.CanonicalPdfUrl != null
                        && references.Contains(book.CanonicalPdfUrl))
                    || (book.CanonicalWordDocUrl != null
                        && references.Contains(book.CanonicalWordDocUrl)))
                .Select(book => new
                {
                    book.CanonicalPdfUrl,
                    book.CanonicalWordDocUrl
                })
                .ToListAsync(cancellationToken);

            return matches
                .SelectMany(match => new[]
                {
                    match.CanonicalPdfUrl,
                    match.CanonicalWordDocUrl
                })
                .Where(reference => reference is not null)
                .Select(reference => reference!)
                .ToHashSet(StringComparer.Ordinal);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);

        // PostgreSQL error code 23505 = unique_violation
        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: "23505" };
    }
}
