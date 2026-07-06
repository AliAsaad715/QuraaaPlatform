using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Authentication.Commands.AdminLogin;
using Quraaa.Application.Features.Authentication.Commands.LibraryOwnerLogin;
using Quraaa.Application.Features.Authentication.Commands.Login;
using Quraaa.API.Requests.Authentication;
using Quraaa.Application.Features.Authentication.Commands.Register;
using Quraaa.Application.Features.Authentication.Commands.ResetPassword;
using Quraaa.Application.Features.Authentication.Commands.ForgotPassword;
using Quraaa.Application.Features.Authentication.Commands.ResetForgotPassword;
using Quraaa.Application.Features.Authentication.Commands.VerifyAdminLoginOtp;
using Quraaa.Application.Features.Authentication.Commands.VerifyRegisterOtp;
using Quraaa.Application.Features.Authentication.Common;
using System.Security.Claims;

namespace Quraaa.API.Controllers
{
    public class AuthController : ApiClientController
    {
        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var command = new RegisterCommand(
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.Password,
                request.Gender,
                request.DateOfBirth,
                request.Interests,
                GetClientIpAddress() ?? string.Empty);

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Authenticates a user and returns an access token.
        /// </summary>
        /// <remarks>
        /// 💡 **Demo Accounts for Testing:**
        /// 
        /// * **User Account:**
        ///     * **Phone Number:** `+963912345678`
        ///     
        ///     * **Password:** `User@12345`
        /// 
        /// * **Admin Account:**
        ///     * **Phone Number:** `+963987654321`
        ///     
        ///     * **Password:** `Admin@12345`
        /// </remarks>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            var result = await Mediator.Send(command);
            return HandleResult(result);
        }


        [AllowAnonymous]
        [HttpPost("library/login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LibraryOwnerLogin([FromBody] LibraryOwnerLoginRequest request)
        {
            var command = new LibraryOwnerLoginCommand(
                request.Email,
                request.Password,
                GetClientIpAddress() ?? string.Empty);

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpPost("admin/login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AdminLogin([FromBody] AdminLoginRequest request)
        {
            var command = new AdminLoginCommand(
                request.PhoneNumber,
                request.Password,
                GetClientIpAddress() ?? string.Empty);

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpPost("admin/login/verify")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyAdminLoginOtp([FromBody] VerifyAdminLoginOtpRequest request)
        {
            var command = new VerifyAdminLoginOtpCommand(
                request.PhoneNumber,
                request.OtpCode,
                GetClientIpAddress() ?? string.Empty);

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
        [HttpPost("register/verify")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyRegisterOtpRequest request)
        {
            var command = new VerifyRegisterOtpCommand(
                request.PhoneNumber,
                request.OtpCode,
                GetClientIpAddress() ?? string.Empty);

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var command = new ForgotPasswordCommand(
                request.PhoneNumber,
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

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("nameid")
                ?? User.FindFirstValue("sub");

            return Guid.TryParse(claimValue, out userId);
        }
    }
}
