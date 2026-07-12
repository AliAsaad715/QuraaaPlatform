using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.FavoriteBooks;
using Quraaa.Application.Features.FavoriteBooks.Commands.AddFavoriteBook;
using Quraaa.Application.Features.FavoriteBooks.Commands.RemoveFavoriteBook;
using Quraaa.Application.Features.FavoriteBooks.Common;
using Quraaa.Application.Features.FavoriteBooks.Queries.GetFavoriteBooks;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    [Authorize]
    public class FavoriteBooksController : ApiClientController
    {
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<FavoriteBookResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetFavoriteBooks(
            [FromQuery] GetFavoriteBooksRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var query = new GetFavoriteBooksQuery(
                userId,
                request.PageNumber,
                request.PageSize,
                request.SearchTerm);

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("{bookId:guid}")]
        [ProducesResponseType(typeof(FavoriteBookResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddFavoriteBook(
            [FromRoute] Guid bookId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new AddFavoriteBookCommand(userId, bookId),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpDelete("{bookId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveFavoriteBook(
            [FromRoute] Guid bookId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new RemoveFavoriteBookCommand(userId, bookId),
                cancellationToken);

            return HandleResult(result);
        }
    }
}
