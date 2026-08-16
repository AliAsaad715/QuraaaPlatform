using MediatR;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Admin.Commands.CreateSuperAdmin
{
    /// <summary>
    /// Super admin command: creates another super admin. Restricted to super
    /// admins, because it hands out the authority to create administrators.
    /// </summary>
    public record CreateSuperAdminCommand : IRequest<AppResult<AdminUserResponse>>
    {
        [JsonIgnore]
        public Guid CreatedByUserId { get; init; }

        public required string FirstName { get; init; }
        public required string LastName { get; init; }

        /// <summary>Syrian mobile number in international form.</summary>
        public required string PhoneNumber { get; init; }

        public required string Password { get; init; }
        public required string ConfirmPassword { get; init; }
    }
}
