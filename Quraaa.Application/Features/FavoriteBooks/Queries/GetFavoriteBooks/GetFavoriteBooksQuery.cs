using MediatR;
using Quraaa.Application.Features.FavoriteBooks.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.FavoriteBooks.Queries.GetFavoriteBooks
{
    public record GetFavoriteBooksQuery(
        Guid UserId,
        int PageNumber = 1,
        int PageSize = 20,
        string? SearchTerm = null) : IRequest<AppResult<PagedResult<FavoriteBookResponse>>>;
}
