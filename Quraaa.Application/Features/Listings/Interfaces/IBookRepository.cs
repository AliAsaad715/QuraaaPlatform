using Quraaa.Domain.Catalog;

namespace Quraaa.Application.Features.Listings.Interfaces
{
    public interface IBookRepository
    {
        Task<BookAggregate?> FindByIsbnAsync(string isbn,
            CancellationToken cancellationToken = default);

        Task<BookAggregate?> FindByTitleAuthorLanguageAsync(
            string title, string author, string language,
            CancellationToken cancellationToken = default);

        Task AddAsync(BookAggregate book,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns (Title, Author, Language) for every book whose lower-cased title
        /// matches any entry in <paramref name="normalizedTitles"/> — in a single roundtrip.
        /// The caller narrows to precise matches via
        /// <see cref="Quraaa.Domain.Catalog.BookTextNormalizer.CompositeKey"/>.
        /// </summary>
        Task<IReadOnlyList<(string Title, string Author, string Language)>> FindExistingCandidatesAsync(
            IReadOnlyList<string> normalizedTitles,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Atomically inserts a batch of books.
        /// Throws <see cref="Quraaa.Domain.Shared.Exceptions.ConflictException"/> on a
        /// (Title, Author, Language) unique-constraint violation.
        /// </summary>
        Task BulkInsertAsync(IReadOnlyList<BookAggregate> books,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}