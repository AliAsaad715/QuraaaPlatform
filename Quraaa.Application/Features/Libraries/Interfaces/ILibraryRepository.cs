using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Queries.GetLibraryBooks;
using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryRepository
    {
        Task<bool> ExistsByUserIdAsync(Guid userId);
        Task AddLibraryAsync(LibraryAggregate library);
        Task<(IReadOnlyCollection<LibraryAggregate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);
        Task<(IReadOnlyCollection<LibraryBookResponse> Items, int TotalCount)> GetLibraryBooksAsync(
            Guid libraryId,
            int pageNumber,
            int pageSize,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = false,
            CancellationToken cancellationToken = default);
        Task<bool> ExistsByIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync();
    }
}
