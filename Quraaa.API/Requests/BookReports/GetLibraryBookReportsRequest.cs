using Quraaa.Domain.Reports.Enums;

namespace Quraaa.API.Requests.BookReports
{
    public class GetLibraryBookReportsRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        /// <summary>Omit to see every status.</summary>
        public BookReportStatus? Status { get; set; }

        /// <summary>Narrow to a single one of the library's books.</summary>
        public Guid? BookId { get; set; }
    }
}
