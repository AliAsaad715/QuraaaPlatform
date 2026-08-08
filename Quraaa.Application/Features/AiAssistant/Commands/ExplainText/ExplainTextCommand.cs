using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.AiAssistant.Commands.ExplainText
{
    public record ExplainTextCommand : IRequest<AppResult<ExplainTextResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        // PurchaseId rather than BookId: the handler resolves the book THROUGH the
        // caller's own purchase record (IDOR prevention), then uses its title/author
        // to ground the AI prompt in what the reader is actually reading.
        public Guid PurchaseId { get; init; }
        public string SelectedText { get; init; } = null!;
    }

    public record ExplainTextResponse(string Explanation);
}