using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.AiAssistant.Commands.TranslateText
{
    public record TranslateTextCommand : IRequest<AppResult<TranslateTextResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }
        public string Text { get; init; } = null!;
        // "ar" or "en" — scoped exactly to FR-AI-02 ("EN to AR, AR to EN"),
        // not an open-ended language field. Source language is inferred as
        // "the other one" rather than asked for separately.
        public string TargetLanguage { get; init; } = null!;
    }

    public record TranslateTextResponse(string TranslatedText);
}