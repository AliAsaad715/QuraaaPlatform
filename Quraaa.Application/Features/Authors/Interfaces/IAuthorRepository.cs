using Quraaa.Application.Features.Authors.Common;
using Quraaa.Domain.Author;

namespace Quraaa.Application.Features.Authors.Interfaces
{
    public interface IAuthorRepository
    {
        Task<AuthorAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<AuthorAggregate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for distinct authors by name among books currently visible in the
        /// catalog, reporting how many such books each author has.
        /// </summary>
        Task<(IReadOnlyCollection<AuthorSearchResponse> Items, int TotalCount)> SearchAsync(
            string? searchTerm,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the existing author matching <paramref name="name"/> (case-insensitive,
        /// trimmed), or stages a new one that the caller's unit of work saves together
        /// with the book that references it. Used by single-book catalog paths (ISBN
        /// lookup, manual listing creation) that resolve one author at a time.
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
        /// Stages new authors without saving, so the caller's unit of work commits them
        /// atomically with sibling entities (e.g. newly catalogued books).
        /// </summary>
        Task AddRangeAsync(IReadOnlyList<AuthorAggregate> authors, CancellationToken cancellationToken = default);

        Task AddAsync(AuthorAggregate author, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stages the deletion. The unit-of-work save throws
        /// <see cref="Quraaa.Domain.Shared.Exceptions.ConflictException"/> while books
        /// still reference the author.
        /// </summary>
        Task RemoveAsync(AuthorAggregate author, CancellationToken cancellationToken = default);
    }
}
