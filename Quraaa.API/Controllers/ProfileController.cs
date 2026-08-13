using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Profiles;
using Quraaa.Application.Features.Profiles.Commands.CreateLocation;
using Quraaa.Application.Features.Profiles.Commands.DeleteLocation;
using Quraaa.Application.Features.Profiles.Commands.SetDefaultLocation;
using Quraaa.Application.Features.Profiles.Commands.UpdateLocation;
using Quraaa.Application.Features.Profiles.Commands.UpdateProfile;
using Quraaa.Application.Features.Profiles.Common;
using Quraaa.Application.Features.Profiles.Queries.GetMyLocations;
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        [HttpGet("locations")]
        [ProducesResponseType(
            typeof(IReadOnlyCollection<LocationResponse>),
            StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyLocations(
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new GetMyLocationsQuery(userId),
                cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("locations")]
        [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateLocation(
            [FromBody] CreateProfileLocationRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new CreateLocationCommand(
                    userId,
                    request.Name,
                    request.Address,
                    request.Latitude,
                    request.Longitude,
                    request.IsDefault),
                cancellationToken);

            return HandleResult(
                result,
                location => StatusCode(StatusCodes.Status201Created, location));
        }

        [HttpPut("locations/{locationId:guid}")]
        [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateLocation(
            Guid locationId,
            [FromBody] UpdateProfileLocationRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new UpdateLocationCommand(
                    userId,
                    locationId,
                    request.Name,
                    request.Address,
                    request.Latitude,
                    request.Longitude),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpPut("locations/{locationId:guid}/default")]
        [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> SetDefaultLocation(
            Guid locationId,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new SetDefaultLocationCommand(userId, locationId),
                cancellationToken);

            return HandleResult(result);
        }

        [HttpDelete("locations/{locationId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteLocation(
            Guid locationId,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(
                new DeleteLocationCommand(userId, locationId),
                cancellationToken);
            return HandleResult(result);
        }
    }
}
