using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Profiles;
using Quraaa.Application.Features.Profiles.Commands.CreateLocation;
using Quraaa.Application.Features.Profiles.Commands.DeleteLocation;
using Quraaa.Application.Features.Profiles.Commands.UpdateProfile;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Features.Profiles.Queries.GetMyProfile;

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
                return InvalidUserIdResult();
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
                return InvalidUserIdResult();
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

        [HttpPost("upsert-location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpsertLocation([FromBody] UpsertLocationCommand command)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }
            var result = await Mediator.Send(command with { UserId = userId });
            return HandleResult(result);
        }

        [HttpDelete("delete-location")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteLocation([FromBody] DeleteLocationCommand command)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }
            var result = await Mediator.Send(command with { UserId = userId });
            return HandleResult(result);
        }
    }
}
