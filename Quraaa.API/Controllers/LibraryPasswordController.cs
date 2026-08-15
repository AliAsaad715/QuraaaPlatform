using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Quraaa.API.Extensions;
using Quraaa.API.Requests.Libraries;
using Quraaa.Application.Features.Libraries.Commands.ChangeLibraryPassword;
using Quraaa.Application.Features.Libraries.Commands.RequestLibraryPasswordReset;
using Quraaa.Application.Features.Libraries.Commands.ResetLibraryPassword;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// The library dashboard password: its own credential, separate from the
    /// owner's personal account password.
    /// </summary>
    [Route("api/library-admin/password")]
    public class LibraryPasswordController : ApiClientController
    {
        // ── PUT /api/library-admin/password ──────────────────────────────────
        /// <summary>
        /// Changes the dashboard password of the caller's library. Requires the
        /// current password, and the new one must differ from both it and the
        /// owner's account password.
        /// </summary>
        /// <response code="200">The password was changed.</response>
        /// <response code="400">The current password is wrong, or the new one breaks a rule.</response>
        /// <response code="401">Not authenticated.</response>
        /// <response code="404">The caller has no approved library.</response>
        [HttpPut]
        [Authorize(Roles = nameof(Role.LibraryOwner))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword(
            [FromBody] ChangeLibraryPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new ChangeLibraryPasswordCommand
                {
                    UserId = userId,
                    CurrentPassword = request.CurrentPassword,
                    NewPassword = request.NewPassword,
                    ConfirmNewPassword = request.ConfirmNewPassword,
                },
                cancellationToken);

            return HandleResult(result);
        }

        // ── POST /api/library-admin/password/forgot ──────────────────────────
        /// <summary>
        /// Emails a six-digit code to the library address so its owner can set a
        /// new dashboard password. The response is identical whether or not the
        /// address belongs to a library, so it reveals nothing (the send happens
        /// inline, so response time still differs). Calling it again re-sends
        /// the current code until it expires.
        /// </summary>
        /// <response code="200">A code was sent if the address belongs to a library.</response>
        /// <response code="400">The email is missing or malformed.</response>
        /// <response code="429">Too many requests; retry later.</response>
        [HttpPost("forgot")]
        [AllowAnonymous]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryPasswordResetRateLimitPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] LibraryForgotPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            var result = await Mediator.Send(
                new RequestLibraryPasswordResetCommand(request.Email),
                cancellationToken);

            return HandleResult(result);
        }

        // ── POST /api/library-admin/password/reset ───────────────────────────
        /// <summary>
        /// Sets a new dashboard password using the code emailed by
        /// password/forgot. The code is single-use, expires, and locks out after
        /// repeated wrong attempts.
        /// </summary>
        /// <response code="200">The password was reset; sign in with it.</response>
        /// <response code="400">The code is wrong or expired, or the password breaks a rule.</response>
        /// <response code="409">Too many wrong attempts; wait before asking for a new code.</response>
        /// <response code="429">Too many requests; retry later.</response>
        [HttpPost("reset")]
        [AllowAnonymous]
        [EnableRateLimiting(ServiceCollectionExtensions.LibraryPasswordResetRateLimitPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetLibraryPasswordRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            var result = await Mediator.Send(
                new ResetLibraryPasswordCommand
                {
                    Email = request.Email,
                    OtpCode = request.OtpCode,
                    NewPassword = request.NewPassword,
                    ConfirmNewPassword = request.ConfirmNewPassword,
                },
                cancellationToken);

            return HandleResult(result);
        }
    }
}
