using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Quraaa.Application.Features.Authentication.Commands.AdminLogin;
using Quraaa.Application.Features.Authentication.Commands.LibraryOwnerLogin;
using Quraaa.Application.Features.Authentication.Commands.Login;
using Quraaa.Application.Features.Authentication.Commands.Logout;
using Quraaa.Application.Features.Authentication.Commands.RefreshToken;
using Quraaa.API.Requests.Authentication;
using Quraaa.Application.Features.Authentication.Commands.Register;
using Quraaa.Application.Features.Authentication.Commands.ResetPassword;
using Quraaa.Application.Features.Authentication.Commands.ForgotPassword;
using Quraaa.Application.Features.Authentication.Commands.ResetForgotPassword;
using Quraaa.Application.Features.Authentication.Commands.VerifyAdminLoginOtp;
using Quraaa.Application.Features.Authentication.Commands.VerifyRegisterOtp;
using Quraaa.Application.Features.Authentication.Common;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
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
        /// Valid credentials for an unverified pending registration resend the
        /// registration OTP. The client should then continue through
        /// <c>POST /api/auth/register/verify</c>.
        ///
        /// 💡 **Demo Accounts for Testing:**
        /// 
        /// * **User Account:**
        ///     * **Phone Number:** `+963912345678`
        ///     
        ///     * **Password:** `User@12345`
        /// 
        /// Admin accounts must use <c>POST /api/auth/admin/login</c> and
        /// <c>POST /api/auth/admin/login/verify</c>.
        /// </remarks>
        [HttpPost("login")]
        [EnableRateLimiting("regular-login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var command = new LoginCommand(
                request.PhoneNumber,
                request.Password,
                GetClientIpAddress() ?? string.Empty);

            var result = await Mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Revokes a user, admin, or library-owner session using its refresh token.
        /// </summary>
        /// <remarks>
        /// A valid bearer token is optional. When present, its access-token id is
        /// also revoked; an expired bearer token does not prevent refresh-token logout.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout(
            [FromBody] LogoutRequest request,
            CancellationToken cancellationToken)
        {
            string? tokenId = null;
            DateTimeOffset? expiresAt = null;

            if (User.Identity?.IsAuthenticated == true
                && TryGetCurrentAccessToken(out var currentTokenId, out var currentExpiresAt))
            {
                tokenId = currentTokenId;
                expiresAt = currentExpiresAt;
            }

            var result = await Mediator.Send(
                new LogoutCommand(request.RefreshToken, tokenId, expiresAt),
                cancellationToken);

            return HandleResult(result);
        }

        /// <summary>
        /// Exchanges a valid refresh token for a new access/refresh token pair.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken(
            [FromBody] RefreshTokenRequest request,
            CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(
                new RefreshTokenCommand(request.RefreshToken),
                cancellationToken);

            return HandleResult(result);
        }


        [AllowAnonymous]
        [HttpPost("library/login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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
                return InvalidUserIdResult();
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
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        private bool TryGetCurrentAccessToken(
            out string tokenId,
            out DateTimeOffset expiresAt)
        {
            tokenId = User.FindFirstValue(JwtRegisteredClaimNames.Jti)
                ?? string.Empty;

            var expirationValue = User.FindFirstValue(JwtRegisteredClaimNames.Exp)
                ?? User.FindFirstValue(ClaimTypes.Expiration);

            if (string.IsNullOrWhiteSpace(tokenId)
                || string.IsNullOrWhiteSpace(expirationValue))
            {
                expiresAt = default;
                return false;
            }

            if (long.TryParse(
                    expirationValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var unixTimeSeconds))
            {
                try
                {
                    expiresAt = DateTimeOffset.FromUnixTimeSeconds(unixTimeSeconds);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    expiresAt = default;
                    return false;
                }
            }

            return DateTimeOffset.TryParse(
                expirationValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out expiresAt);
        }
    }
}
