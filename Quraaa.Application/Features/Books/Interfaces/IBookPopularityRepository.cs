using Quraaa.Application.Features.Books.Common;

namespace Quraaa.Application.Features.Books.Interfaces
{
    public interface IBookPopularityRepository
    {
        Task<(IReadOnlyCollection<PopularBookResponse> Items, int TotalCount)> GetMostPopularAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            string sortBy,
            bool includeUnranked,
            CancellationToken cancellationToken = default);
    }
}
