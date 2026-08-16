using MediatR;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Admin.Commands.SetLibraryActivation
{
    /// <summary>
    /// Administrator command: deactivates or reactivates libraries.
    /// Deactivation is a soft delete — the record leaves every ordinary query
    /// but stays recoverable, and it is the only state from which a permanent
    /// delete is allowed.
    /// </summary>
    public record SetLibraryActivationCommand : IRequest<AppResult<BulkModerationResult>>
    {
        [JsonIgnore]
        public Guid AdminId { get; init; }

        /// <summary>One id for a single action, many for a bulk one.</summary>
        public required IReadOnlyCollection<Guid> Ids { get; init; }

        /// <summary>True deactivates; false brings the records back.</summary>
        public required bool Deactivate { get; init; }
    }
}
