using MediatR;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Libraries.Commands.DeleteOwnLibrary
{
    /// <summary>
    /// Library owner command: permanently deletes their own library. Guarded by
    /// the library dashboard password and a typed confirmation phrase, and
    /// refused while anything still references the library.
    /// </summary>
    public record DeleteOwnLibraryCommand : IRequest<AppResult>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        /// <summary>The library dashboard password, not the personal one.</summary>
        public required string Password { get; init; }

        public required string ConfirmationPhrase { get; init; }
    }
}
