using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Listings;
using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;
using Quraaa.Application.Features.Listings.Commands.AddUserPhysicalBook;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Features.Listings.Queries.GetMyListings;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    [Authorize(Roles = "User")]
    [ApiController]
    [Route("api/listings")]
    public class UserListingsController : ApiClientController
    {
        /// <summary>
        /// Get a paged list of the authenticated user's own listings.
        /// </summary>
        /// <remarks>
        /// Each item includes <c>Version</c>, an integer counter incremented every time
        /// the listing's price, stock, condition, or digital asset changes.
        /// </remarks>
        [HttpGet("me")]
        [ProducesResponseType(typeof(PagedResult<ListingSummaryResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyListings(
            [FromQuery] GetMyListingsRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }
            var query = new GetMyListingsQuery(
                    userId,
                    request.SearchTerm,
                    request.SortBy,
                    request.SortDescending
                );
            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("me/physical")]
        [ProducesResponseType(typeof(AddPhysicalBookResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddPhysicalBook(
            [FromBody] AddUserPhysicalBookCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(command with { RequestingUserId = userId }, cancellationToken);
            return HandleResult(result);
        }
    }
}