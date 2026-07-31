using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Libraries.Commands.UpdateLibraryApprovalStatus;
using Quraaa.Application.Shared.Results;

namespace Quraaa.API.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/libraries")]
    public class AdminController : ApiClientController
    {
        [HttpPatch("{id:guid}/approval-status")]
        [ProducesResponseType(typeof(AppResult<bool>), StatusCodes.Status200OK)]
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
    }
}