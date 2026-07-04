using MediatR;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Marketplace.Enums;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Listings.Commands.AddPhysicalBook
{
    public record AddPhysicalBookCommand : IRequest<AppResult<AddPhysicalBookResponse>>
    {
        // ── Set from JWT in the controller — never from the request body ──────
        [JsonIgnore]
        public Guid RequestingUserId { get; init; }

        // ── Always required ───────────────────────────────────────────────────
        public decimal Price { get; init; }
        public int Quantity { get; init; }
        public BookCondition Condition { get; init; }

        // ── ISBN path (optional) ──────────────────────────────────────────────
        // System will:  DB lookup → Google Books API
        public string? Isbn { get; init; }
    }
}