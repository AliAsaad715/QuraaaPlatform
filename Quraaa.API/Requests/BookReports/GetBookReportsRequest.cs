using Quraaa.Domain.Reports.Enums;

namespace Quraaa.API.Requests.BookReports
{
    public class GetBookReportsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        /// <summary>Defaults to Pending; omit to see every status.</summary>
        public BookReportStatus? Status { get; set; } = BookReportStatus.Pending;

        public Guid? BookId { get; set; }
        public Guid? ReporterUserId { get; set; }
        public string? SearchTerm { get; set; }
    }
}
