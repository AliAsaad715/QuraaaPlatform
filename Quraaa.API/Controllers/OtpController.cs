using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.API.Requests.Otp;
using Quraaa.Application.Features.Otp.Commands.SendOtp;
using Quraaa.Application.Features.Otp.Commands.VerifyOtp;

namespace Quraaa.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OtpController : ApiClientController
    {
        private const string OtpDeviceTokenConfigurationKey = "OTP_DEVICE_TOKEN";

        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;

        public OtpController(IMediator mediator, IConfiguration configuration)
        {
            _mediator = mediator;
            _configuration = configuration;
        }

        [HttpPost("send")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
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

            var command = new SendOtpCommand(
                request.PhoneNumber,
                smsGatewayDeviceToken,
                GetClientIpAddress());
            var result = await _mediator.Send(command);

            return HandleResult(result);
        }

        [HttpPost("verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var command = new VerifyOtpCommand(request.PhoneNumber, request.Code, GetClientIpAddress());
            var result = await _mediator.Send(command);

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
    }
}
