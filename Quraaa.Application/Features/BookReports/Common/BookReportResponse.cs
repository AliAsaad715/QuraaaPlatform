using Quraaa.Domain.Reports.Enums;

namespace Quraaa.Application.Features.BookReports.Common
{
    /// <summary>
    /// A submitted report, as shown to the reporter and to moderators.
    /// </summary>
    public record BookReportResponse(
        Guid ReportId,
        Guid BookId,
        string BookTitle,
        string BookAuthor,
        string? BookCoverImageUrl,
        Guid ReporterUserId,
        string ReporterName,
        BookReportReason Reason,
        string? Details,
        BookReportStatus Status,
        string? ModeratorNote,
        Guid? ReviewedByAdminId,
        DateTime? ReviewedAtUtc,
        DateTime CreatedAt);
}
