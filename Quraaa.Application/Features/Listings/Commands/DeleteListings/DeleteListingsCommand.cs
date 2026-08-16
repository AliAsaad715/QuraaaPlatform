using MediatR;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Listings.Commands.DeleteListings
{
    /// <summary>
    /// Library owner command: permanently deletes their own listings. Allowed
    /// only for listings already removed from sale and never bought, ordered, or
    /// held in a cart.
    /// </summary>
    public record DeleteListingsCommand : IRequest<AppResult<BulkModerationResult>>
    {
        [JsonIgnore]
        public Guid RequestingUserId { get; init; }

        public required IReadOnlyCollection<Guid> Ids { get; init; }
    }
}
