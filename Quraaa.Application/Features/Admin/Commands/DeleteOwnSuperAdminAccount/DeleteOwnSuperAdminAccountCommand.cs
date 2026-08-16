using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Admin.Commands.DeleteOwnSuperAdminAccount
{
    /// <summary>
    /// Super admin command: permanently deletes the caller's own account.
    /// Guarded by the account password and a typed confirmation phrase, and
    /// refused when it would leave the platform with no super admin.
    /// </summary>
    public record DeleteOwnSuperAdminAccountCommand : IRequest<AppResult>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        public required string Password { get; init; }

        /// <summary>Must be typed exactly; see AccountDeletionConfirmation.</summary>
        public required string ConfirmationPhrase { get; init; }
    }
}
