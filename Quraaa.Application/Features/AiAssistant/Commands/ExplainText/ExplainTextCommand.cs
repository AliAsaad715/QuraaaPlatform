using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.AiAssistant.Commands.ExplainText
{
    public record ExplainTextCommand : IRequest<AppResult<ExplainTextResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }
        public string Text { get; init; } = null!;
    }

    public record ExplainTextResponse(string Explanation);
}