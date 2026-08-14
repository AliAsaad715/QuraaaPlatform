using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Quraaa.API.Extensions;
using Quraaa.Application.Features.Payouts.Commands.RemoveLibraryWallet;
using Quraaa.Application.Features.Payouts.Commands.SetLibraryWallet;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Exceptions;
using Quraaa.Application.Features.Payouts.Queries.GetLibraryWallet;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Lets an approved library owner manage the Stripe wallet (Stripe Connect
    /// account) that receives their share of profits when orders containing
    /// their listings are paid.
    /// </summary>
    [Authorize(Roles = "LibraryOwner")]
    [ApiController]
    [Route("api/library-admin/wallet")]
    public class LibraryWalletController : ApiClientController
    {
        // ── GET /api/library-admin/wallet ────────────────────────────────────
        /// <summary>
        /// Returns the Stripe wallet currently configured for the caller's
        /// approved library, if any.
        /// </summary>
        /// <response code="200">The wallet state was returned successfully.</response>
        /// <response code="404">The caller has no approved library.</response>
        [HttpGet]
        [ProducesResponseType(typeof(LibraryWalletResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetWallet(
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new GetLibraryWalletQuery(userId),
                cancellationToken);

            return HandleResult(result);
        }

        // ── PUT /api/library-admin/wallet ────────────────────────────────────
        /// <summary>
        /// Adds or replaces the caller's Stripe wallet. The Stripe Connect
        /// account id is verified with Stripe (it must exist, be connected to
        /// the platform, and be able to receive transfers) before it is saved.
        /// Pending profit-share payouts that were waiting for a wallet are
        /// released immediately after a successful save.
        /// </summary>
        /// <param name="command">The Stripe Connect account id (acct_...).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">The wallet was saved successfully.</response>
        /// <response code="400">The account id is invalid or cannot receive transfers.</response>
        /// <response code="404">The caller has no approved library.</response>
        /// <response code="503">Stripe could not be reached to verify the account; retry later.</response>
        [HttpPut]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryWalletRateLimitPolicy)]
        [ProducesResponseType(typeof(LibraryWalletResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> SetWallet(
            [FromBody] SetLibraryWalletCommand command,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            try
            {
                var result = await Mediator.Send(
                    command with { UserId = userId },
                    cancellationToken);

                return HandleResult(result);
            }
            catch (PayoutGatewayException)
            {
                // A Stripe-side outage — the account id itself was never
                // evaluated, so answer 503 rather than a 400 field error.
                Response.Headers.RetryAfter = "60";

                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    type = "ServiceUnavailable",
                    title = "Stripe Unavailable",
                    detail = "The Stripe account could not be verified right now. Please try again later."
                });
            }
        }

        // ── DELETE /api/library-admin/wallet ─────────────────────────────────
        /// <summary>
        /// Removes the caller's Stripe wallet. Profit shares from later paid
        /// orders are held until a new wallet is configured.
        /// </summary>
        /// <response code="200">The wallet was removed (or none was configured).</response>
        /// <response code="404">The caller has no approved library.</response>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveWallet(
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new RemoveLibraryWalletCommand(userId),
                cancellationToken);

            return HandleResult(result);
        }

        private void SetNoStoreHeaders()
        {
            Response.Headers.CacheControl = "no-store";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Append("Referrer-Policy", "no-referrer");
        }
    }
}
