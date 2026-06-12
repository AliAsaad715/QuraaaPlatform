using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Authentication.Commands.Login;
using Quraaa.API.Requests.Authentication;
using Quraaa.Application.Features.Authentication.Commands.Register;
using Quraaa.Application.Features.Authentication.Commands.ResetPassword;
using Quraaa.Application.Features.Authentication.Common;
using System.Security.Claims;

namespace Quraaa.API.Controllers
{
    public class AuthController : ApiClientController
    {
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [Authorize]
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
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

            var command = new ResetPasswordCommand(
                userId,
                request.OldPassword,
                request.NewPassword
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
