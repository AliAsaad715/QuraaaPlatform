using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Quraaa.API.Extensions;
using Quraaa.Application.Features.Payouts.Commands.CompleteLibraryWalletOnboarding;
using Quraaa.Application.Features.Payouts.Commands.CreateLibraryWalletDashboardLink;
using Quraaa.Application.Features.Payouts.Commands.RemoveLibraryWallet;
using Quraaa.Application.Features.Payouts.Commands.SetLibraryWallet;
using Quraaa.Application.Features.Payouts.Commands.StartLibraryWalletOnboarding;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Queries.GetLibraryWallet;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Lets an approved library owner manage the Stripe wallet (Stripe Connect
    /// account) that receives their share of profits when orders containing
    /// their listings are paid. The primary way to add a wallet is
    /// Stripe-hosted onboarding (POST onboarding, then POST onboarding/complete
    /// on return); attaching an existing account id (PUT) is also supported.
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

        // ── POST /api/library-admin/wallet/onboarding ────────────────────────
        /// <summary>
        /// Starts (or resumes) Stripe-hosted onboarding for the caller's wallet.
        /// On first use the platform creates a Stripe Express account for the
        /// library; the owner then completes identity and bank details on
        /// Stripe. The client must redirect the owner to the returned URL. When
        /// Stripe sends the owner back, call POST onboarding/complete.
        /// </summary>
        /// <param name="command">Optional return/refresh URLs on an allow-listed frontend origin.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <response code="200">Redirect the owner to the returned onboarding URL.</response>
        /// <response code="400">A redirect URL is not on an allowed frontend origin.</response>
        /// <response code="404">The caller has no approved library.</response>
        /// <response code="409">The wallet is already active; remove it to connect another.</response>
        /// <response code="429">Too many onboarding attempts; retry later.</response>
        /// <response code="502">Stripe rejected the request; the detail explains why.</response>
        /// <response code="503">Stripe could not be reached; retry later.</response>
        [HttpPost("onboarding")]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryWalletRateLimitPolicy)]
        [ProducesResponseType(typeof(LibraryStripeOnboardingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> StartOnboarding(
            [FromBody] StartLibraryWalletOnboardingCommand? command,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            return await SendWithPayoutGatewayMappingAsync(
                (command ?? new StartLibraryWalletOnboardingCommand()) with { UserId = userId },
                cancellationToken);
        }

        // ── POST /api/library-admin/wallet/onboarding/complete ───────────────
        /// <summary>
        /// Called when the owner returns from Stripe onboarding. Re-checks the
        /// wallet with Stripe; if it can receive transfers it becomes Active and
        /// any profit shares waiting for it are transferred immediately.
        /// Idempotent — safe to call on every return/refresh.
        /// </summary>
        /// <response code="200">The current wallet state after synchronizing with Stripe.</response>
        /// <response code="404">The caller has no approved library.</response>
        /// <response code="429">Too many status checks; retry later.</response>
        /// <response code="502">Stripe rejected the request; the detail explains why.</response>
        /// <response code="503">Stripe could not be reached; retry later.</response>
        [HttpPost("onboarding/complete")]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryWalletSyncRateLimitPolicy)]
        [ProducesResponseType(typeof(LibraryWalletResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> CompleteOnboarding(
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            return await SendWithPayoutGatewayMappingAsync(
                new CompleteLibraryWalletOnboardingCommand(userId),
                cancellationToken);
        }

        // ── POST /api/library-admin/wallet/dashboard-link ────────────────────
        /// <summary>
        /// Creates a short-lived link to the owner's Stripe Express dashboard,
        /// where they edit bank details and view Stripe-side payouts. The
        /// client must open the returned URL right away.
        /// </summary>
        /// <response code="200">Open the returned URL.</response>
        /// <response code="400">No wallet yet, or the account type has no hosted dashboard.</response>
        /// <response code="404">The caller has no approved library.</response>
        /// <response code="429">Too many requests; retry later.</response>
        /// <response code="502">Stripe rejected the request; the detail explains why.</response>
        /// <response code="503">Stripe could not be reached; retry later.</response>
        [HttpPost("dashboard-link")]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryWalletRateLimitPolicy)]
        [ProducesResponseType(typeof(LibraryWalletDashboardLinkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> CreateDashboardLink(
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            return await SendWithPayoutGatewayMappingAsync(
                new CreateLibraryWalletDashboardLinkCommand(userId),
                cancellationToken);
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
        /// <response code="429">Too many attempts; retry later.</response>
        /// <response code="502">Stripe rejected the request; the detail explains why.</response>
        /// <response code="503">Stripe could not be reached to verify the account; retry later.</response>
        [HttpPut]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryWalletRateLimitPolicy)]
        [ProducesResponseType(typeof(LibraryWalletResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
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

            // A provider failure is mapped by the base controller: the account
            // id was never evaluated, so it must not surface as a field error.
            return await SendWithPayoutGatewayMappingAsync(
                command with { UserId = userId },
                cancellationToken);
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

    }
}
