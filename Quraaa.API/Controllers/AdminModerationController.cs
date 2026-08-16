using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Admin;
using Quraaa.Application.Features.Admin.Commands.DeleteAuthors;
using Quraaa.Application.Features.Admin.Commands.DeleteLibraries;
using Quraaa.Application.Features.Admin.Commands.DeleteUsers;
using Quraaa.Application.Features.Admin.Commands.SetAuthorActivation;
using Quraaa.Application.Features.Admin.Commands.SetLibraryActivation;
using Quraaa.Application.Features.Admin.Commands.SetUserActivation;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Queries.GetAuthors;
using Quraaa.Application.Features.Admin.Queries.GetLibraries;
using Quraaa.Application.Features.Admin.Queries.GetUsers;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.User.Enums;

namespace Quraaa.API.Controllers
{
    /// <summary>
    /// Administrator management of accounts, authors, and libraries.
    ///
    /// Two-stage removal throughout: deactivating a record soft-deletes it — it
    /// vanishes from the platform but stays recoverable — and only a record that
    /// is already deactivated AND that nothing references can be permanently
    /// deleted. Every action works on one record or many.
    /// </summary>
    [Route("api/admin")]
    [Authorize(Roles = nameof(Role.SuperAdmin))]
    public class AdminModerationController : ApiClientController
    {

        // ══════════════════════════ users ══════════════════════════

        /// <summary>
        /// Lists users. Deactivated records are hidden unless
        /// <c>includeDeactivated</c> is set.
        /// </summary>
        /// <response code="200">A paged collection of users.</response>
        [HttpGet("users")]
        [ProducesResponseType(typeof(PagedResult<AdminUserResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetUsers(
            [FromQuery] GetAdminUsersRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetUsersQuery
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SearchTerm = request.SearchTerm,
                    IncludeDeactivated = request.IncludeDeactivated,
                    Role = request.Role,
                },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Deactivates one user (soft delete) or brings it back with
        /// <c>?deactivate=false</c>.
        /// </summary>
        /// <response code="200">The outcome for the record.</response>
        [HttpPost("users/{id:guid}/activation")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetUserActivation(
            [FromRoute] Guid id,
            [FromQuery] bool deactivate = true,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new SetUserActivationCommand { AdminId = adminId, Ids = [id], Deactivate = deactivate },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Deactivates or reactivates many users at once. Records that cannot
        /// be changed are reported in the result rather than failing the call.
        /// </summary>
        /// <response code="200">Per-record outcomes.</response>
        [HttpPost("users/activation")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetUsersActivation(
            [FromBody] BulkActivationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new SetUserActivationCommand
                {
                    AdminId = adminId,
                    Ids = request.Ids,
                    Deactivate = request.Deactivate,
                },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Permanently deletes one user. Refused unless it is already
        /// deactivated and nothing references it; the response names whatever
        /// still does.
        /// </summary>
        /// <response code="200">The outcome, including any blocking references.</response>
        [HttpDelete("users/{id:guid}")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUser(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new DeleteUsersCommand { AdminId = adminId, Ids = [id] },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Permanently deletes many users. Each record is checked on its own;
        /// the ones that cannot go are reported and left untouched.
        /// </summary>
        /// <response code="200">Per-record outcomes.</response>
        [HttpDelete("users")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUsers(
            [FromBody] BulkIdsRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new DeleteUsersCommand { AdminId = adminId, Ids = request.Ids },
                cancellationToken);

            return HandleResult(result);
        }

        // ══════════════════════════ authors ══════════════════════════

        /// <summary>
        /// Lists authors. Deactivated records are hidden unless
        /// <c>includeDeactivated</c> is set.
        /// </summary>
        /// <response code="200">A paged collection of authors.</response>
        [HttpGet("authors")]
        [ProducesResponseType(typeof(PagedResult<AdminAuthorResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetAuthors(
            [FromQuery] GetAdminRecordsRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetAuthorsQuery
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SearchTerm = request.SearchTerm,
                    IncludeDeactivated = request.IncludeDeactivated,
                },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Deactivates one author (soft delete) or brings it back with
        /// <c>?deactivate=false</c>.
        /// </summary>
        /// <response code="200">The outcome for the record.</response>
        [HttpPost("authors/{id:guid}/activation")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetAuthorActivation(
            [FromRoute] Guid id,
            [FromQuery] bool deactivate = true,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new SetAuthorActivationCommand { AdminId = adminId, Ids = [id], Deactivate = deactivate },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Deactivates or reactivates many authors at once. Records that cannot
        /// be changed are reported in the result rather than failing the call.
        /// </summary>
        /// <response code="200">Per-record outcomes.</response>
        [HttpPost("authors/activation")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetAuthorsActivation(
            [FromBody] BulkActivationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new SetAuthorActivationCommand
                {
                    AdminId = adminId,
                    Ids = request.Ids,
                    Deactivate = request.Deactivate,
                },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Permanently deletes one author. Refused unless it is already
        /// deactivated and nothing references it; the response names whatever
        /// still does.
        /// </summary>
        /// <response code="200">The outcome, including any blocking references.</response>
        [HttpDelete("authors/{id:guid}")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAuthor(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new DeleteAuthorsCommand { AdminId = adminId, Ids = [id] },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Permanently deletes many authors. Each record is checked on its own;
        /// the ones that cannot go are reported and left untouched.
        /// </summary>
        /// <response code="200">Per-record outcomes.</response>
        [HttpDelete("authors")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteAuthors(
            [FromBody] BulkIdsRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new DeleteAuthorsCommand { AdminId = adminId, Ids = request.Ids },
                cancellationToken);

            return HandleResult(result);
        }

        // ══════════════════════════ libraries ══════════════════════════

        /// <summary>
        /// Lists libraries. Deactivated records are hidden unless
        /// <c>includeDeactivated</c> is set.
        /// </summary>
        /// <response code="200">A paged collection of libraries.</response>
        [HttpGet("libraries")]
        [ProducesResponseType(typeof(PagedResult<AdminLibraryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetLibraries(
            [FromQuery] GetAdminRecordsRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await Mediator.Send(
                new GetLibrariesQuery
                {
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    SearchTerm = request.SearchTerm,
                    IncludeDeactivated = request.IncludeDeactivated,
                },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Deactivates one library (soft delete) or brings it back with
        /// <c>?deactivate=false</c>.
        /// </summary>
        /// <response code="200">The outcome for the record.</response>
        [HttpPost("libraries/{id:guid}/activation")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetLibraryActivation(
            [FromRoute] Guid id,
            [FromQuery] bool deactivate = true,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new SetLibraryActivationCommand { AdminId = adminId, Ids = [id], Deactivate = deactivate },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Deactivates or reactivates many libraries at once. Records that cannot
        /// be changed are reported in the result rather than failing the call.
        /// </summary>
        /// <response code="200">Per-record outcomes.</response>
        [HttpPost("libraries/activation")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SetLibrariesActivation(
            [FromBody] BulkActivationRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new SetLibraryActivationCommand
                {
                    AdminId = adminId,
                    Ids = request.Ids,
                    Deactivate = request.Deactivate,
                },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Permanently deletes one library. Refused unless it is already
        /// deactivated and nothing references it; the response names whatever
        /// still does.
        /// </summary>
        /// <response code="200">The outcome, including any blocking references.</response>
        [HttpDelete("libraries/{id:guid}")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteLibrary(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new DeleteLibrariesCommand { AdminId = adminId, Ids = [id] },
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Permanently deletes many libraries. Each record is checked on its own;
        /// the ones that cannot go are reported and left untouched.
        /// </summary>
        /// <response code="200">Per-record outcomes.</response>
        [HttpDelete("libraries")]
        [ProducesResponseType(typeof(BulkModerationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteLibraries(
            [FromBody] BulkIdsRequest request,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var adminId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new DeleteLibrariesCommand { AdminId = adminId, Ids = request.Ids },
                cancellationToken);

            return HandleResult(result);
        }
    }
}
