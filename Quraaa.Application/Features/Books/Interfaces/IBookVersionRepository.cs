using Quraaa.Domain.Catalog;

namespace Quraaa.Application.Features.Books.Interfaces
{
    /// <summary>
    /// Reads book history. There is no way to add a version here: only
    /// <see cref="BookAggregate"/> records them, and they are saved with the book.
    /// </summary>
    public interface IBookVersionRepository
    {
        Task<BookAggregate?> GetBookForUpdateAsync(
            Guid bookId,
            CancellationToken cancellationToken = default);

        Task<BookVersion?> GetVersionAsync(
            Guid bookId,
            int versionNumber,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<BookVersion>> GetVersionsAsync(
            Guid bookId,
            CancellationToken cancellationToken = default);

        Task<bool> HasAnyVersionAsync(
            Guid bookId,
            CancellationToken cancellationToken = default);
    }
}
