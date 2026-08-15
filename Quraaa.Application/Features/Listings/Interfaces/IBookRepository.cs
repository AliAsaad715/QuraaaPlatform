using Quraaa.Domain.Catalog;
using Quraaa.Domain.Catalog.Enums;

namespace Quraaa.Application.Features.Listings.Interfaces
{
    public interface IBookRepository
    {
        Task<BookAggregate?> FindByIsbnAsync(string isbn,
            CancellationToken cancellationToken = default);

        Task<BookAggregate?> GetByIdAsync(Guid id,
            CancellationToken cancellationToken = default);

        Task<BookAggregate?> FindByTitleAuthorLanguageAsync(
            string title, string author, Language language,
            CancellationToken cancellationToken = default);

        Task AddAsync(BookAggregate book,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns (Title, Author, Language) for every book whose lower-cased title
        /// matches any entry in <paramref name="normalizedTitles"/> — in a single roundtrip.
        /// Author is resolved via a left join to Authors and is null for a book with no
        /// linked author. The caller narrows to precise matches via
        /// <see cref="Quraaa.Domain.Catalog.BookTextNormalizer.CompositeKey"/>.
        /// </summary>
        Task<IReadOnlyList<(string Title, string? Author, Language Language)>> FindExistingCandidatesAsync(
            IReadOnlyList<string> normalizedTitles,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically inserts a batch of books.
        /// Throws <see cref="Quraaa.Domain.Shared.Exceptions.ConflictException"/> on a
        /// (Title, Author, Language) unique-constraint violation.
        /// </summary>
        Task BulkInsertAsync(IReadOnlyList<BookAggregate> books,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns storage references still owned by a book's canonical PDF or Word
        /// columns. Retention cleanup must preserve these even when no digital listing
        /// currently points at the same canonical file.
        /// </summary>
        Task<HashSet<string>> FilterReferencedCanonicalAssetPathsAsync(
            IReadOnlyCollection<string> storedReferences,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
