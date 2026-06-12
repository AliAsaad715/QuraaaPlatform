using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Files;
using Quraaa.API.Requests.Libraries;
using Quraaa.Application.Features.Libraries.Commands.RegisterLibrary;
using Quraaa.Application.Features.Libraries.Common;
using System.Security.Claims;

namespace Quraaa.API.Controllers
{
    public class LibraryController : ApiClientController
    {
        [Authorize]
        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(LibraryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Register([FromForm] RegisterLibraryRequest request)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return Unauthorized(new
                {
                    type = "Unauthorized",
                    title = "Invalid Authentication Token",
                    detail = "The authentication token does not contain a valid user id."
                });
            }

            var command = new RegisterLibraryCommand(
                request.LibraryName,
                request.Location,
                request.LibraryImage is null ? null : new FormFileUploadedFile(request.LibraryImage),
                request.HeaderImage is null ? null : new FormFileUploadedFile(request.HeaderImage),
                request.Email,
                userId
            );

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("nameid")
                ?? User.FindFirstValue("sub");

            return Guid.TryParse(claimValue, out userId);
        }
    }
}
