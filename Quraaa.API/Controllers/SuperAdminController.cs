using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Admin;
using Quraaa.Application.Features.Admin.Commands.CreateSuperAdmin;
using Quraaa.Application.Features.Admin.Commands.DeleteOwnSuperAdminAccount;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Actions only a super admin may perform.
    /// </summary>
    [Route("api/admin/super-admins")]
    [Authorize(Roles = nameof(Role.SuperAdmin))]
    public class SuperAdminController : ApiClientController
    {
        // ── POST /api/admin/super-admins ─────────────────────────────────────
        /// <summary>
        /// Creates another super admin. The new account can sign in through the
        /// administrator login immediately and holds full platform authority,
        /// including the ability to create further super admins.
        /// </summary>
        /// <response code="201">The new super admin.</response>
        /// <response code="400">A field is invalid, or the password is too weak.</response>
        /// <response code="403">The caller is not a super admin.</response>
        /// <response code="409">The phone number already belongs to an account.</response>
        [HttpPost]
        [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateSuperAdmin(
            [FromBody] CreateSuperAdminRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new CreateSuperAdminCommand
                {
                    CreatedByUserId = userId,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Password = request.Password,
                    ConfirmPassword = request.ConfirmPassword,
                },
                cancellationToken);

            return HandleResult(
                result,
                created => StatusCode(StatusCodes.Status201Created, created));
        }

        // ── DELETE /api/admin/super-admins/me ────────────────────────────────
        /// <summary>
        /// Permanently deletes the caller's own super admin account. Requires
        /// the account password and the exact phrase "DELETE MY ACCOUNT".
        /// Refused if it would leave no super admin, or while anything still
        /// references the account. This cannot be undone.
        /// </summary>
        /// <response code="200">The account was deleted; the session is over.</response>
        /// <response code="400">The password or confirmation phrase is wrong.</response>
        /// <response code="409">Last super admin, or the account is still referenced.</response>
        [HttpDelete("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteOwnAccount(
            [FromBody] DeleteOwnAccountRequest request,
            CancellationToken cancellationToken = default)
        {
            SetNoStoreHeaders();

            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new DeleteOwnSuperAdminAccountCommand
                {
                    UserId = userId,
                    Password = request.Password,
                    ConfirmationPhrase = request.ConfirmationPhrase,
                },
                cancellationToken);

            return HandleResult(result);
        }
    }
}
