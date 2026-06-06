using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.Authentication.Commands.Register;
using Quraaa.Application.Features.Authentication.Common;

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
    }
}
