using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Controllers;
using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;
using Quraaa.Application.Features.Listings.Commands.RemoveListing;
using Quraaa.Application.Features.Listings.Commands.ReactivateListing;
using Quraaa.Application.Features.Listings.Commands.UpdateListing;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Features.Listings.Queries.GetListingById;
using Quraaa.Application.Features.Listings.Queries.GetMyLibraryListings;
using Quraaa.Application.Shared.Results;
using Quraaa.API.Requests.Libraries;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// All endpoints require the caller to be an authenticated LibraryOwner
    /// whose library is already approved.
    /// </summary>
    [Authorize(Roles = "LibraryOwner")]
    [ApiController]
    [Route("api/library-admin/listings")]
    public class LibraryListingsController : ApiClientController
    {
        // ── POST /api/library-admin/listings ─────────────────────────────────
        /// <summary>
        /// Adds a physical book to the current user's library.
        /// </summary>
        /// <param name="command">The book data (ISBN, condition, etc.).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The unique identifier (GUID) of the newly added listing.</returns>
        /// <remarks>
        /// ISBN lookup resolution order:
        /// <list type="number">
        /// <item><description>Local Books table (by ISBN).</description></item>
        /// <item><description>Google Books API.</description></item>
        /// </list>
        /// Allowed values for <c>BookCondition</c>:
        /// <c>New = 1, LikeNew = 2, Good = 3, Acceptable = 4.</c>
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddPhysicalBook(
            [FromBody] AddPhysicalBookCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with { RequestingUserId = userId },
                cancellationToken);

            return HandleResult(result);
        }

        // ── PUT /api/library-admin/listings/{listingId} ───────────────────────
        /// <summary>
        /// Update price, stock, and/or condition for a listing.
        /// Only fields that are present in the body are updated (partial update).
        /// </summary>
        [HttpPut("{listingId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateListing(
            [FromRoute] Guid listingId,
            [FromBody] UpdateListingCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with
                {
                    ListingId = listingId,
                    RequestingUserId = userId
                },
                cancellationToken);

            return HandleResult(result);
        }

        // ── GET /api/library-admin/listings/me ───────────────────────────────
        /// <summary>
        /// Get a paged list of listings for the authenticated library owner's approved library.
        /// </summary>
        /// <remarks>
        /// The response includes <c>Status</c> so frontend clients can render listing state.
        /// Supported values are <c>Active = 1</c>, <c>OutOfStock = 2</c>, and <c>Removed = 4</c>.
        /// </remarks>
        [HttpGet("me")]
        [ProducesResponseType(typeof(PagedResult<ListingSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyListings(
            [FromQuery] GetLibraryBooksRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var query = new GetMyLibraryListingsQuery(
                userId,
                request.SearchTerm,
                request.SortBy,
                request.SortDescending,
                request.Status)
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        // ── DELETE /api/library-admin/listings/{listingId} ───────────────────
        /// <summary>
        /// Remove a listing from the authenticated library owner's approved library.
        /// </summary>
        [HttpDelete("{listingId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> RemoveListing(
            [FromRoute] Guid listingId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new RemoveListingCommand(userId, listingId),
                cancellationToken);

            return HandleResult(result);
        }

        // ── PATCH /api/library-admin/listings/{listingId}/activate ─────────────
        /// <summary>
        /// Reactivate a previously removed listing owned by the authenticated library.
        /// </summary>
        [HttpPatch("{listingId:guid}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ReactivateListing(
            [FromRoute] Guid listingId,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new ReactivateListingCommand(userId, listingId),
                cancellationToken);

            return HandleResult(result);
        }

        // ── GET /api/library-admin/listings/{listingId} ───────────────────────
        /// <summary>
        /// Get full listing details (listing + book + category) by listing ID.
        /// </summary>
        [HttpGet("{listingId:guid}")]
        [ProducesResponseType(typeof(ListingDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetListingById(
            [FromRoute] Guid listingId,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetListingByIdQuery(listingId),
                cancellationToken);

            return HandleResult(result);
        }
    }
}
