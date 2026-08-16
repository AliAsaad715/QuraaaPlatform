using MediatR;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Listings.Commands.SetListingsActivation
{
    /// <summary>
    /// Library owner command: removes listings from sale, or puts them back.
    /// Removal is reversible and is the only state a listing can be permanently
    /// deleted from.
    /// </summary>
    public record SetListingsActivationCommand : IRequest<AppResult<BulkModerationResult>>
    {
        [JsonIgnore]
        public Guid RequestingUserId { get; init; }

        public required IReadOnlyCollection<Guid> Ids { get; init; }

        /// <summary>True removes the listings from sale; false restores them.</summary>
        public required bool Deactivate { get; init; }
    }
}
