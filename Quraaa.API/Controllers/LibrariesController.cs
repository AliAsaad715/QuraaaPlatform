using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Extensions;
using Quraaa.API.Requests.Files;
using Quraaa.API.Requests.Libraries;
using Quraaa.Application.Features.Libraries.Commands.RegisterLibrary;
using Quraaa.Application.Features.Libraries.Commands.IssueLibraryRegistrationLink;
using Quraaa.Application.Features.Libraries.Commands.ResendLibraryEmailOtp;
using Quraaa.Application.Features.Libraries.Commands.StartRegistrationStripeOnboarding;
using Quraaa.Application.Features.Libraries.Commands.SyncRegistrationStripeWallet;
using Quraaa.Application.Features.Libraries.Commands.UpdateLibraryApprovalStatus;
using Quraaa.Application.Features.Libraries.Commands.VerifyLibraryEmailOtp;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Queries.GetLibraries;
using Quraaa.Application.Features.Libraries.Queries.SearchLibraries;
using Quraaa.Application.Features.Libraries.Queries.GetLibraryRequests;
using Quraaa.Application.Features.Libraries.Queries.GetLibraryRegistrationContext;
using Quraaa.Application.Features.Libraries.Queries.GetMyProfile;
using Quraaa.Application.Features.Listings.Queries.GetLibraryBooks;
using Quraaa.Application.Features.Payouts.Commands.SetLibraryProfitShare;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Queries.GetLibraryProfitShare;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers
{
    public class LibrariesController : ApiClientController
    {
        /// <summary>
        /// Issues a temporary, single-purpose link to the library dashboard.
        /// Reissuing this link invalidates the previous registration link.
        /// </summary>
        [Authorize(Roles = "User")]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryRegistrationLinkRateLimitPolicy)]
        [HttpPost("register")]
        [ProducesResponseType(typeof(LibraryRegistrationLinkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> IssueRegistrationLink(CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId)
                || !TryGetCurrentSessionId(out var authenticationSessionId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new IssueLibraryRegistrationLinkCommand(userId, authenticationSessionId),
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Resolves the current dashboard registration stage without exposing the owner id.
        /// The token must be sent in the JSON body so it is not written to API query logs.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryRegistrationPublicRateLimitPolicy)]
        [HttpPost("register/context")]
        [ProducesResponseType(typeof(LibraryRegistrationContextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetRegistrationContext(
            [FromBody] LibraryRegistrationTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();
            var result = await Mediator.Send(
                new GetLibraryRegistrationContextQuery(request.Token),
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Submits library details from the dashboard using the temporary registration token.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryRegistrationPublicRateLimitPolicy)]
        [HttpPost("register/submit")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(LibraryRegistrationSubmissionResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> SubmitRegistration(
            [FromForm] RegisterLibraryRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();
            var command = new RegisterLibraryCommand(
                request.Token,
                request.LibraryName,
                request.Location,
                request.LibraryImage is null ? null : new FormFileUploadedFile(request.LibraryImage),
                request.HeaderImage is null ? null : new FormFileUploadedFile(request.HeaderImage),
                request.Email,
                request.Password,
                request.ConfirmPassword
            );

            var result = await Mediator.Send(command, cancellationToken);
            return HandleResult(result, data => Accepted(data));
        }

        /// <summary>
        /// Sends a replacement email OTP for an already submitted registration.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryRegistrationPublicRateLimitPolicy)]
        [HttpPost("register/email/resend")]
        [ProducesResponseType(typeof(LibraryEmailOtpResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResendRegistrationEmailOtp(
            [FromBody] LibraryRegistrationTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();
            var result = await Mediator.Send(
                new ResendLibraryEmailOtpCommand(request.Token),
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Verifies the submitted library email and moves the application into admin review.
        /// </summary>
        [AllowAnonymous]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryRegistrationPublicRateLimitPolicy)]
        [HttpPost("register/email/verify")]
        [ProducesResponseType(typeof(LibraryEmailVerificationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> VerifyRegistrationEmail(
            [FromBody] VerifyLibraryEmailRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();
            var result = await Mediator.Send(
                new VerifyLibraryEmailOtpCommand(
                    request.Token,
                    request.VerificationId,
                    request.OtpCode),
                cancellationToken);

            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PublicLibraryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLibraries([FromQuery] GetLibrariesQuery query)
        {
            var result = await Mediator.Send(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Searches for approved libraries by name, returning each match's active listing count.
        /// </summary>
        /// <response code="200">A paged collection of matching libraries was returned.</response>
        /// <response code="400">The pagination or search input is invalid.</response>
        [AllowAnonymous]
        [HttpGet("search")]
        [ProducesResponseType(typeof(PagedResult<LibrarySearchResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SearchLibraries(
            [FromQuery] SearchLibrariesRequest request,
            CancellationToken cancellationToken = default)
        {
            var query = new SearchLibrariesQuery(request.SearchTerm, request.PageNumber, request.PageSize);
            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Get a paged list of books available in a specific library.
        /// </summary>
        /// <param name="libraryId" example="01f185c0-dff4-45fa-8fe6-60d1c870ea8b">The unique identifier of the library (Pre-loaded example containing books for testing(FrontEnd)).</param>
        /// <param name="request">Pagination, filtering, and sorting parameters.</param>
        /// <param name="cancellationToken"></param>
        /// <remarks>
        /// Each item includes <c>Version</c>, an integer counter incremented every time
        /// the listing's price, stock, condition, or digital asset changes.
        /// </remarks>
        [AllowAnonymous]
        [HttpGet("{libraryId}/books")]
        [ProducesResponseType(typeof(PagedResult<ListingSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLibraryBooks(
        [FromRoute] Guid libraryId,
        [FromQuery] GetLibraryBooksRequest request,
        CancellationToken cancellationToken = default)
        {
            var query = new GetLibraryBooksQuery
            {
                LibraryId = libraryId,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                SortBy = request.SortBy,
                SortDescending = request.SortDescending
            };
            var result = await Mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves the profile of the currently authenticated library owner.
        /// </summary>
        /// <response code="200">The profile was found and returned successfully.</response>
        /// <response code="404">No profile exists for the authenticated user.</response>
        [HttpGet("my-profile")]
        [Authorize(Roles = "LibraryOwner")]
        [ProducesResponseType(typeof(MyProfileLibraryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile(
        CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(new GetMyProfileQuery(userId), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Registration wizard, Stripe step (after email verification): starts
        /// or resumes Stripe-hosted onboarding for the new library's wallet.
        /// The dashboard must redirect the owner to the returned URL, and call
        /// register/stripe/sync when Stripe sends them back. Optional — the
        /// owner can also connect Stripe later from the owner dashboard once
        /// the library is approved. Authenticated by the registration token in
        /// the JSON body.
        /// </summary>
        /// <response code="200">Redirect the owner to the returned onboarding URL.</response>
        /// <response code="400">Email not verified yet, or a redirect URL is not on an allowed origin.</response>
        /// <response code="401">The registration token is invalid, expired, or revoked.</response>
        /// <response code="409">The wallet is already active.</response>
        /// <response code="502">Stripe rejected the request; the detail explains why.</response>
        /// <response code="503">Stripe could not be reached; retry later.</response>
        [AllowAnonymous]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryWalletRateLimitPolicy)]
        [HttpPost("register/stripe/onboarding")]
        [ProducesResponseType(typeof(LibraryStripeOnboardingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> StartRegistrationStripeOnboarding(
            [FromBody] LibraryRegistrationStripeOnboardingRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            return await SendWithPayoutGatewayMappingAsync(
                new StartRegistrationStripeOnboardingCommand(
                    request.Token,
                    request.ReturnUrl,
                    request.RefreshUrl),
                cancellationToken);
        }

        /// <summary>
        /// Registration wizard, Stripe step: called when the owner returns from
        /// Stripe onboarding. Re-checks the wallet with Stripe; once it can
        /// receive transfers the wallet becomes Active and the registration
        /// wizard is completed. Idempotent. Authenticated by the registration
        /// token in the JSON body.
        /// </summary>
        /// <response code="200">The wallet state after synchronizing with Stripe.</response>
        /// <response code="401">The registration token is invalid, expired, or revoked.</response>
        /// <response code="502">Stripe rejected the request; the detail explains why.</response>
        /// <response code="503">Stripe could not be reached; retry later.</response>
        [AllowAnonymous]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryRegistrationPublicRateLimitPolicy)]
        [HttpPost("register/stripe/sync")]
        [ProducesResponseType(typeof(LibraryWalletResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> SyncRegistrationStripeWallet(
            [FromBody] LibraryRegistrationTokenRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            return await SendWithPayoutGatewayMappingAsync(
                new SyncRegistrationStripeWalletCommand(request.Token),
                cancellationToken);
        }

        /// <summary>
        /// Retrieves a paged list of library registration requests. Only accessible by administrators.
        /// </summary>
        /// <param name="request">
        /// Pagination, filtering, and sorting parameters.
        /// Filter by status using the LibraryApprovalStatus enum (1=Pending, 2=Approved, 3=Rejected).
        /// AwaitingEmailVerification (4) applications are intentionally not exposed to admins.
        /// </param>
        /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
        /// <returns>
        /// An IActionResult containing a paged result of LibraryRequestResponse objects.
        /// </returns>
        /// <remarks>
        /// LibraryApprovalStatus values:
        /// - Pending (1): Awaiting admin review.
        /// - Approved (2): Request has been approved.
        /// - Rejected (3): Request has been rejected.
        /// - AwaitingEmailVerification (4): Hidden until the applicant verifies the library email.
        /// </remarks>
        /// <response code="200">A paged collection of library requests was returned successfully.</response>
        [HttpGet("requests")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(PagedResult<LibraryRequestResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRequests(
            [FromQuery] GetLibraryRequestsQuery request,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(request, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Updates the approval status of a library by only admin.
        /// </summary>
        /// <param name="id">The library identifier.</param>
        /// <param name="command">The request containing the new approval status.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>Returns the result of the update operation.</returns>
        /// <response code="200">Approval status updated successfully.</response>
        /// <response code="400">The request is invalid.</response>
        /// <response code="401">Unauthorized.</response>
        /// <response code="403">Forbidden.</response>
        /// <response code="404">Library not found.</response>
        [HttpPatch("{id:guid}/approval-status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateApprovalStatus(
            [FromRoute] Guid id,
            [FromBody] UpdateLibraryApprovalStatusCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with { LibraryId = id, AdminId = adminId },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Gets the profit-share percentage of a library — the share of its
        /// gross sales paid out to the library owner on every paid order. Only
        /// accessible by administrators.
        /// </summary>
        /// <param name="id">The library identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <response code="200">The library's current profit share.</response>
        /// <response code="404">Library not found.</response>
        [HttpGet("{id:guid}/profit-share")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(LibraryProfitShareResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfitShare(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetLibraryProfitShareQuery(id),
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Sets the profit-share percentage of a library — the share of its
        /// gross sales that is automatically transferred to the library owner's
        /// Stripe wallet when an order is paid. The platform keeps the
        /// remainder. Applies to orders paid from now on. Only accessible by
        /// administrators.
        /// </summary>
        /// <param name="id">The library identifier.</param>
        /// <param name="command">The new percentage (0–100, up to 4 decimal places).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <response code="200">The profit share was updated.</response>
        /// <response code="400">The percentage is out of range or too precise.</response>
        /// <response code="404">Library not found.</response>
        /// <response code="409">The library changed concurrently; retry.</response>
        [HttpPut("{id:guid}/profit-share")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(LibraryProfitShareResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SetProfitShare(
            [FromRoute] Guid id,
            [FromBody] SetLibraryProfitShareCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                command with { LibraryId = id, AdminId = adminId },
                cancellationToken);

            return HandleResult(result);
        }

    }
}
