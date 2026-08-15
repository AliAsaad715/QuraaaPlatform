using Quraaa.Application.Features.Books.Common;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Application.Features.Books.Interfaces
{
    public interface IHomeCatalogRepository
    {
        Task<(IReadOnlyCollection<HomeBookResponse> Items, int TotalCount)> GetCatalogAsync(
            string? searchTerm,
            Guid? categoryId,
            Guid? libraryId,
            SellerType? sellerType,
            ListingFormat? format,
            bool? isFree,
            BookCondition? condition,
            decimal? minPrice,
            decimal? maxPrice,
            string sortBy,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<HomeBookResponse> Items, int TotalCount)> GetByAuthorAsync(
            Guid authorId,
            string? searchTerm,
            string sortBy,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}
