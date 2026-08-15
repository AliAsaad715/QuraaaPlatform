using Quraaa.Application.Features.Books.Common;
using Quraaa.Domain.Catalog.Enums;

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

        Task<(IReadOnlyCollection<PopularBookResponse> Items, int TotalCount)> GetRecommendedAsync(
            IReadOnlyCollection<Guid> interestedCategoryIds,
            Language language,
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);
    }
}
