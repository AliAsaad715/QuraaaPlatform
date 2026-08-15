using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Files;
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

        /// <summary>
        /// List a physical book the authenticated user owns for sale.
        /// </summary>
        /// <remarks>
        /// A cover/condition photo of the actual item is required — it is shown to buyers
        /// instead of the book's generic catalog cover, since user-sold physical copies
        /// vary in condition.
        /// </remarks>
        [HttpPost("me/physical")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(AddPhysicalBookResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddPhysicalBook(
            [FromForm] AddUserPhysicalBookRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var command = new AddUserPhysicalBookCommand
            {
                RequestingUserId = userId,
                Price = request.Price,
                Condition = request.Condition,
                Isbn = request.Isbn,
                CoverImage = new FormFileUploadedFile(request.CoverImage)
            };

            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }
}