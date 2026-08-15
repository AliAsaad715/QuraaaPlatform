using Quraaa.Domain.Reports.Enums;

namespace Quraaa.Application.Features.BookReports.Common
{
    /// <summary>
    /// One selectable reason for the report form.
    /// </summary>
    /// <param name="Reason">The value to send back when creating a report.</param>
    /// <param name="NameEn">English label.</param>
    /// <param name="NameAr">Arabic label.</param>
    /// <param name="RequiresDetails">
    /// When true the reporter must also describe the problem.
    /// </param>
    public record BookReportReasonResponse(
        BookReportReason Reason,
        string NameEn,
        string NameAr,
        bool RequiresDetails);
}
