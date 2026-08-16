using MediatR;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Admin.Commands.DeleteUsers
{
    /// <summary>
    /// Administrator command: permanently removes users. Only a
    /// record that is already deactivated AND that nothing references can be
    /// removed, so nothing disappears by accident and no history is orphaned.
    /// </summary>
    public record DeleteUsersCommand : IRequest<AppResult<BulkModerationResult>>
    {
        [JsonIgnore]
        public Guid AdminId { get; init; }

        /// <summary>One id for a single delete, many for a bulk one.</summary>
        public required IReadOnlyCollection<Guid> Ids { get; init; }
    }
}
