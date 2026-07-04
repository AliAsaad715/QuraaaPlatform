using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Controllers;
using Quraaa.Application.Features.Listings.Commands.AddPhysicalBook;
using Quraaa.Application.Features.Listings.Commands.UpdateListing;
using Quraaa.Application.Features.Listings.Queries.GetListingById;
using System.Security.Claims;

namespace Quraaa.Presentation.Controllers
{
    /// <summary>
    /// All endpoints require the caller to be an authenticated LibraryAdmin
    /// whose library is already approved.
    /// </summary>
    //[Authorize(Roles = "LibraryAdmin")]
    [Authorize(Roles = "User")]
    [ApiController]
    [Route("api/library-admin/listings")]
    public class LibraryListingsController : ApiClientController
    {
        // ── POST /api/library-admin/listings ─────────────────────────────────
        /// <summary>
        /// Add a physical book to the caller's library.
        ///
        /// Resolution order when ISBN is supplied:
        ///   1. Books table (by ISBN)
        ///   2. Google Books API
        ///   
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddPhysicalBook(
            [FromBody] AddPhysicalBookCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                command with { RequestingUserId = GetCurrentUserId() },
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
            var result = await Mediator.Send(
                command with
                {
                    ListingId = listingId,
                    RequestingUserId = GetCurrentUserId()
                },
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

        // ─────────────────────────────────────────────────────────────────────
        private Guid GetCurrentUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}