using Quraaa.Application.Features.FavoriteBooks.Common;
using Quraaa.Domain.Favorites;

namespace Quraaa.Application.Features.FavoriteBooks.Interfaces
{
    public interface IFavoriteBookRepository
    {
        Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default);
        Task<FavoriteBookResponse?> GetFavoriteAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
        Task<(IReadOnlyCollection<FavoriteBookResponse> Items, int TotalCount)> GetPagedAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);
        Task AddAsync(FavoriteBookAggregate favoriteBook, CancellationToken cancellationToken = default);
        Task<bool> RemoveAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
