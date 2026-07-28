using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quraaa.Application.Features.AiAssistant.Commands.ExplainText;
using Quraaa.Application.Features.AiAssistant.Commands.SummarizeText;
using Quraaa.Application.Features.AiAssistant.Commands.TranslateText;

namespace Quraaa.API.Controllers
{
    // FR-AI-04 (chat panel) is intentionally not here — deferred for MVP.
    [Authorize(Roles = "User")]
    [ApiController]
    [Route("api/ai")]
    public class AiAssistantController : ApiClientController
    {
        [HttpPost("summarize")]
        [ProducesResponseType(typeof(SummarizeTextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Summarize(
            [FromBody] SummarizeTextCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(command with { UserId = userId }, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("translate")]
        [ProducesResponseType(typeof(TranslateTextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Translate(
            [FromBody] TranslateTextCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(command with { UserId = userId }, cancellationToken);
            return HandleResult(result);
        }

        [HttpPost("explain")]
        [ProducesResponseType(typeof(ExplainTextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Explain(
            [FromBody] ExplainTextCommand command,
            CancellationToken cancellationToken = default)
        {
            if (!TryGetCurrentUserId(out var userId))
            {
                return InvalidUserIdResult();
            }

            var result = await Mediator.Send(command with { UserId = userId }, cancellationToken);
            return HandleResult(result);
        }
    }
}