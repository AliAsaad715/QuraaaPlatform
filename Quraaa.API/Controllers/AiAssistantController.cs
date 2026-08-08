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
        // Body is just { "purchaseId": "..." } — the backend resolves the book
        // through the caller's own purchase (never a bare BookId, so this can't be
        // used to summarize a book the caller doesn't own).
        [HttpPost("summarize")]
        [ProducesResponseType(typeof(SummarizeTextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        // Body is just { "purchaseId": "...", "pageNumber": ..., "targetLanguage": "..." } —
        // PurchaseId (not a bare BookId) proves ownership, and the page's text is
        // extracted server-side from that purchase's canonical PDF.
        [HttpPost("translate")]
        [ProducesResponseType(typeof(TranslateTextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        // Body is { "purchaseId": "...", "selectedText": "..." } — PurchaseId (not a
        // bare BookId) proves ownership and grounds the AI prompt in the book's
        // title/author instead of the excerpt alone.
        [HttpPost("explain")]
        [ProducesResponseType(typeof(ExplainTextResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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