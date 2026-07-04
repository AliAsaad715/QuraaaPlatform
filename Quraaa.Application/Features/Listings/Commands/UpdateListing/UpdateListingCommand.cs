using MediatR;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Marketplace.Enums;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Listings.Commands.UpdateListing
{
    /// <summary>
    /// All fields are optional — only supplied fields are updated.
    /// At least one of Price, Stock, or Condition must be present.
    /// </summary>
    public record UpdateListingCommand : IRequest<AppResult>
    {
        [JsonIgnore]
        public Guid ListingId { get; init; }
        [JsonIgnore]
        public Guid RequestingUserId { get; init; }

        public decimal? Price { get; init; }
        public int? Stock { get; init; }
        public BookCondition? Condition { get; init; }
    }
}