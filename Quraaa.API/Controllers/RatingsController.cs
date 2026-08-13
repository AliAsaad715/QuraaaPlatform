using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Ratings;
using Quraaa.Application.Features.Ratings.Commands.RateBook;
using Quraaa.Application.Features.Ratings.Common;
using Quraaa.Application.Features.Ratings.Queries.GetBookRatingSummary;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    [Route("api/books/{bookId:guid}/ratings")]
    public class RatingsController : ApiClientController
    {
        [HttpGet("summary")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(BookRatingSummaryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetRatingSummary(
            [FromRoute] Guid bookId,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetBookRatingSummaryQuery(bookId), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize(Roles = nameof(Role.User))]
        [ProducesResponseType(typeof(BookRatingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RateBook(
            [FromRoute] Guid bookId,
            [FromBody] RateBookRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new RateBookCommand(userId, bookId, request.Score),
                cancellationToken);

            return HandleResult(result);
        }
    }
}
