using Quraaa.Domain.Author;

namespace Quraaa.Application.Features.Authors.Interfaces
{
    public interface IAuthorRepository
    {
        Task<AuthorAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<AuthorAggregate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the existing author matching <paramref name="name"/> (case-insensitive,
        /// trimmed), or creates and immediately persists a new one. Used by single-book
        /// catalog paths (ISBN lookup, manual listing creation) that resolve one author at a time.
        /// </summary>
        Task<AuthorAggregate> FindOrCreateByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns existing authors whose name (case-insensitive, trimmed) matches any of
        /// <paramref name="normalizedNames"/> — in a single roundtrip. Used together with
        /// <see cref="AddRangeAsync"/> for batch author resolution (e.g. bulk book upload).
        /// </summary>
        Task<List<AuthorAggregate>> GetByNormalizedNamesAsync(
            IReadOnlyList<string> normalizedNames,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stages new authors without saving. Callers that need to commit these atomically
        /// alongside sibling entities (e.g. newly catalogued books) call SaveChangesAsync
        /// on a repository sharing the same DbContext once everything is staged.
        /// </summary>
        Task AddRangeAsync(IReadOnlyList<AuthorAggregate> authors, CancellationToken cancellationToken = default);

        Task AddAsync(AuthorAggregate author, CancellationToken cancellationToken = default);
        Task RemoveAsync(AuthorAggregate author, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
