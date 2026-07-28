using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.AiAssistant.Commands.SummarizeText
{
    public record SummarizeTextCommand : IRequest<AppResult<SummarizeTextResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }
        public string Text { get; init; } = null!;
        // Optional context only — nothing currently looks this up server-side
        // (no book content is stored). Kept for future analytics / history
        // without needing to change the request shape later.
        public Guid? BookId { get; init; }
    }

    public record SummarizeTextResponse(string Summary);
}