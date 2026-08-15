using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Reports.Enums;

namespace Quraaa.Application.Features.BookReports.Common
{
    /// <summary>
    /// A report against a book the library lists, as the library owner sees it.
    ///
    /// Deliberately narrower than <see cref="BookReportResponse"/>: the reporter
    /// is never identified, so a seller cannot retaliate against a reader, and
    /// the moderator's internal note stays internal. What the owner gets is what
    /// they can act on — which book, what is wrong, and where it stands.
    /// </summary>
    public record LibraryBookReportResponse(
        Guid ReportId,
        Guid BookId,
        string BookTitle,
        string BookAuthor,
        string? BookCoverImageUrl,
        BookReportReason Reason,
        string? Details,
        BookReportStatus Status,
        DateTime? ReviewedAtUtc,
        DateTime CreatedAt,
        BookModerationStatus BookModerationStatus,
        int BookCurrentVersionNumber);
}
