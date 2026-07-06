using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.FavoriteBooks.Commands.RemoveFavoriteBook
{
    public record RemoveFavoriteBookCommand(Guid UserId, Guid BookId) : IRequest<AppResult>;
}
