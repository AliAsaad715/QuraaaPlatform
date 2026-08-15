using Quraaa.Domain.Catalog;

namespace Quraaa.Application.Features.Books.Interfaces
{
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

        Task AddAsync(
            BookVersion version,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
