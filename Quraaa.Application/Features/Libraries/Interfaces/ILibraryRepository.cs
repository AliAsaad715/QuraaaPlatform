using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryRepository
    {
        Task AddLibraryAsync(LibraryAggregate library);
        Task<(IReadOnlyCollection<LibraryAggregate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);
        Task SaveChangesAsync();
    }
}
