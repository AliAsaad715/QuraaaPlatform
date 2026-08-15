using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Libraries.Commands.ChangeLibraryPassword
{
    /// <summary>
    /// Library owner command: replaces the library dashboard password.
    /// </summary>
    public record ChangeLibraryPasswordCommand : IRequest<AppResult>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        public required string CurrentPassword { get; init; }

        public required string NewPassword { get; init; }

        public required string ConfirmNewPassword { get; init; }
    }
}
