using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Queries.GetLibraryPayouts;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Read-only history of the profit-share payouts generated for the
    /// caller's approved library.
    /// </summary>
    [Authorize(Roles = "LibraryOwner")]
    [ApiController]
    [Route("api/library-admin/payouts")]
    public class LibraryPayoutsController : ApiClientController
    {
        // ── GET /api/library-admin/payouts ───────────────────────────────────
        /// <summary>
        /// Returns a paged list of the caller's payouts, newest first. Each
        /// row shows the gross sale amount, the library's profit-share
        /// percentage applied, the platform's remainder, the net amount
        /// transferred to the wallet, and the payout's current status
        /// (Pending, Paid, Failed, or NoAmountDue when the library's share
        /// rounded to zero).
        /// </summary>
        /// <param name="pageNumber">1-based page number.</param>
        /// <param name="pageSize">Page size, between 1 and 100.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The payouts were returned successfully.</response>
        /// <response code="404">The caller has no approved library.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<SellerPayoutResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPayouts(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new GetLibraryPayoutsQuery(userId, pageNumber, pageSize),
                cancellationToken);

            return HandleResult(result);
        }

    }
}
