using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Reviews;
using Quraaa.Application.Features.Reviews.Commands.CreateBookReview;
using Quraaa.Application.Features.Reviews.Commands.DeleteBookReview;
using Quraaa.Application.Features.Reviews.Commands.UpdateBookReview;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Features.Reviews.Queries.GetBookReviews;
using Quraaa.Application.Features.Reviews.Queries.GetUserBookReview;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    [Route("api/books/{bookId:guid}/reviews")]
    public class ReviewsController : ApiClientController
    {
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PagedResult<BookReviewResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetBookReviews(
            [FromRoute] Guid bookId,
            [FromQuery] GetBookReviewsRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = new GetBookReviewsQuery(bookId, request.PageNumber, request.PageSize);
            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType(typeof(BookReviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserBookReview(
            [FromRoute] Guid bookId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new GetUserBookReviewQuery(userId, bookId), cancellationToken);
            return HandleResult(result);
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(BookReviewResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateReview(
            [FromRoute] Guid bookId,
            [FromBody] CreateBookReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new CreateBookReviewCommand(userId, bookId, request.Score, request.Content),
                cancellationToken);

            return HandleResult(result, data => StatusCode(StatusCodes.Status201Created, data));
        }

        [HttpPut]
        [Authorize]
        [ProducesResponseType(typeof(BookReviewResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReview(
            [FromRoute] Guid bookId,
            [FromBody] UpdateBookReviewRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new UpdateBookReviewCommand(userId, bookId, request.Score, request.Content),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpDelete]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReview(
            [FromRoute] Guid bookId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new DeleteBookReviewCommand(userId, bookId), cancellationToken);
            return HandleResult(result);
        }
    }
}
