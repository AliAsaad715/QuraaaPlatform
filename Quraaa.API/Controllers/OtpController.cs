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
        private readonly IMediator _mediator;

        public OtpController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("send")]
        [AllowAnonymous]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            var command = new SendOtpCommand(
                request.PhoneNumber,
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
    }
}
