using MediatR;
using Quraaa.Application.Features.Books.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Books.Commands.SetBookVisibility
{
    /// <summary>
    /// Administrator command: withholds a book from the catalog, or returns it.
    /// </summary>
    public record SetBookVisibilityCommand : IRequest<AppResult<BookModerationResponse>>
    {
        [JsonIgnore]
        public Guid BookId { get; init; }

        [JsonIgnore]
        public Guid AdminId { get; init; }

        /// <summary>True to withhold the book, false to return it.</summary>
        public required bool Hidden { get; init; }

        public string? ModerationNote { get; init; }
    }
}
