using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Listings.Queries.GetListingDetails;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Public, buyer-facing listing endpoints. Unlike <see cref="UserListingsController"/> and
    /// <see cref="LibraryListingsController"/> (seller-side listing management), these endpoints
    /// are anonymous and read-only.
    /// </summary>
    public class ListingsController : ApiClientController
    {
        // ── GET /api/listings/{id}/details ────────────────────────────────────
        /// <summary>
        /// Get full public details for a single listing, for the mobile app's book details screen.
        /// </summary>
        /// <remarks>
        /// <c>Publisher</c> is the library name for library-sold listings, or the seller's full
        /// name for user-sold listings. <c>Writer</c> is the book's author name. <c>AverageRating</c>
        /// and <c>TotalReviewsCount</c> summarize the book's star ratings; <c>RecentReviews</c> is a
        /// short preview of the book's most recent comments (each comment's <c>Rating</c> is the
        /// same reviewer's star rating for the book, if they left one — otherwise <c>null</c>).
        /// </remarks>
        [AllowAnonymous]
        [HttpGet("{id:guid}/details")]
        [ProducesResponseType(typeof(ListingDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetListingDetails(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(new GetListingDetailsQuery(id), cancellationToken);
            return HandleResult(result);
        }
    }
}
