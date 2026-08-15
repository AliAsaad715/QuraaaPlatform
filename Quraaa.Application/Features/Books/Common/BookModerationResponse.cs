using Quraaa.Domain.Catalog.Enums;

namespace Quraaa.Application.Features.Books.Common
{
    /// <summary>A book's moderation state, as shown to administrators.</summary>
    public record BookModerationResponse(
        Guid BookId,
        string Title,
        BookModerationStatus ModerationStatus,
        int CurrentVersionNumber,
        DateTime? HiddenAtUtc,
        string? ModerationNote,
        int DistinctReporterCount);
}
