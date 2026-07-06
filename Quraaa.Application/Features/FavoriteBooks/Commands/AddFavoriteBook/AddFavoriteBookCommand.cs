using MediatR;
using Quraaa.Application.Features.FavoriteBooks.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.FavoriteBooks.Commands.AddFavoriteBook
{
    public record AddFavoriteBookCommand(Guid UserId, Guid BookId) : IRequest<AppResult<FavoriteBookResponse>>;
}
