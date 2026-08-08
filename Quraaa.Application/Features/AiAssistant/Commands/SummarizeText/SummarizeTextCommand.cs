using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.AiAssistant.Commands.SummarizeText
{
    public record SummarizeTextCommand : IRequest<AppResult<SummarizeTextResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        // PurchaseId rather than BookId: the handler resolves the book THROUGH the
        // caller's own purchase record, so a request can't be used to summarize a
        // book the caller never bought (IDOR prevention).
        public Guid PurchaseId { get; init; }
    }

    public record SummarizeTextResponse(string Summary);
}