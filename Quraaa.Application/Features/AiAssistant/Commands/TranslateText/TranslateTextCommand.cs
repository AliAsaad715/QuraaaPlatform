using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.AiAssistant.Commands.TranslateText
{
    public record TranslateTextCommand : IRequest<AppResult<TranslateTextResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        // PurchaseId rather than BookId: the handler resolves the book THROUGH the
        // caller's own purchase, so a request can't be used to translate a page from
        // a book the caller never bought (IDOR prevention) — same pattern as
        // SummarizeText/ExplainText.
        public Guid PurchaseId { get; init; }

        // 1-based. The mobile PDF viewer can't extract text itself, so it sends the
        // page it's currently displaying and the backend extracts that page's text.
        public int PageNumber { get; init; }

        // "ar" or "en" — scoped exactly to FR-AI-02 ("EN to AR, AR to EN"),
        // not an open-ended language field. Source language is inferred as
        // "the other one" rather than asked for separately.
        public string TargetLanguage { get; init; } = null!;
    }

    public record TranslateTextResponse(string TranslatedText);
}