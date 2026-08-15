using MediatR;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Books.Commands.RevertBookToVersion
{
    /// <summary>
    /// Administrator command: restores the content of an earlier version. The
    /// earlier version is copied forward as a new version, so nothing in the
    /// history is rewritten or lost.
    /// </summary>
    public record RevertBookToVersionCommand : IRequest<AppResult<BookModerationResponse>>
    {
        [JsonIgnore]
        public Guid BookId { get; init; }

        [JsonIgnore]
        public Guid AdminId { get; init; }

        /// <summary>The version whose content should become current.</summary>
        public required int VersionNumber { get; init; }

        /// <summary>Why the revert was performed.</summary>
        public string? ModerationNote { get; init; }
    }
}
