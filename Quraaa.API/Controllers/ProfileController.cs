using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Profiles;
using Quraaa.Application.Features.Profiles.Commands.UpdateProfile;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Features.Profiles.Queries.GetMyProfile;
using System.Security.Claims;

namespace Quraaa.API.Controllers
{
    [Authorize]
    public class ProfileController : ApiClientController
    {
        [HttpGet("me")]
        [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
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

            var result = await Mediator.Send(new GetMyProfileQuery(userId));
            return HandleResult(result);
        }

        [HttpPut("me")]
        [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
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

            var command = new UpdateProfileCommand(
                userId,
                request.FirstName,
                request.LastName,
                request.Gender,
                request.DateOfBirth,
                request.ProfileImageUrl,
                request.Interests
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
