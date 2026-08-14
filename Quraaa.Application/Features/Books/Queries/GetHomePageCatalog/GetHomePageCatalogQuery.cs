using MediatR;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Application.Features.Books.Queries.GetHomePageCatalog
{
    public record GetHomePageCatalogQuery(
        string? SearchTerm = null,
        Guid? CategoryId = null,
        Guid? LibraryId = null,
        SellerType? SellerType = null,
        ListingFormat? Format = null,
        bool? IsFree = null,
        BookCondition? Condition = null,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        string SortBy = "latest",
        int PageNumber = 1,
        int PageSize = 20) : IRequest<AppResult<PagedResult<HomeBookResponse>>>;
}
