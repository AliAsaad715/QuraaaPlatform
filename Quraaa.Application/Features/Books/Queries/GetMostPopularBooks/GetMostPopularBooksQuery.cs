using MediatR;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Books.Queries.GetMostPopularBooks
{
    public record GetMostPopularBooksQuery(
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null,
        string SortBy = "popular",
        bool IncludeUnranked = true) : IRequest<AppResult<PagedResult<PopularBookResponse>>>;
}
