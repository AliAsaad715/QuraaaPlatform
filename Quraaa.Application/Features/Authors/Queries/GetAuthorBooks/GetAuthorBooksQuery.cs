using MediatR;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Authors.Queries.GetAuthorBooks
{
    public sealed record GetAuthorBooksQuery(
        Guid AuthorId,
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null,
        string SortBy = "latest")
        : IRequest<AppResult<PagedResult<HomeBookResponse>>>;
}
