using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Quraaa.Application.Features.Authentication.Commands.Login;
using Quraaa.API.Requests.Authentication;
using Quraaa.Application.Features.Authentication.Commands.ResetPassword;
using Quraaa.Application.Features.Authentication.Commands.ForgotPassword;
using Quraaa.Application.Features.Authentication.Commands.ResetForgotPassword;
using Quraaa.Application.Features.Authentication.Common;
using System.Security.Claims;

namespace Quraaa.API.Controllers
{
    public class AuthController : ApiClientController
    {
        private const string OtpDeviceTokenConfigurationKey = "OTP_DEVICE_TOKEN";

        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

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

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var smsGatewayDeviceToken = GetSmsGatewayDeviceToken();
            if (string.IsNullOrWhiteSpace(smsGatewayDeviceToken))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    type = "ConfigurationError",
                    title = "OTP Gateway Token Missing",
                    detail = "OTP_DEVICE_TOKEN is not configured on the server."
                });
            }

            var command = new ForgotPasswordCommand(
                request.PhoneNumber,
                smsGatewayDeviceToken,
                GetClientIpAddress() ?? string.Empty);

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password/verify")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> VerifyForgotPassword([FromBody] ResetForgotPasswordRequest request)
        {
            var command = new ResetForgotPasswordCommand(
                request.PhoneNumber,
                request.OtpCode,
                request.NewPassword,
                GetClientIpAddress() ?? string.Empty);

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        private string? GetClientIpAddress()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private string? GetSmsGatewayDeviceToken()
        {
            return _configuration[OtpDeviceTokenConfigurationKey]
                ?? Environment.GetEnvironmentVariable(OtpDeviceTokenConfigurationKey);
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
