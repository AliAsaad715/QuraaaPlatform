using MediatR;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Reports.Enums;

namespace Quraaa.Application.Features.BookReports.Queries.GetBookReports
{
    /// <summary>
    /// Administrator query: the moderation queue. Defaults to Pending — the
    /// reports awaiting action — and any status can be requested explicitly;
    /// omit it to see every report.
    /// </summary>
    public record GetBookReportsQuery : IRequest<AppResult<PagedResult<BookReportResponse>>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;
        public BookReportStatus? Status { get; init; } = BookReportStatus.Pending;
        public Guid? BookId { get; init; }
        public Guid? ReporterUserId { get; init; }

        /// <summary>Matches the book title, author, or the report details.</summary>
        public string? SearchTerm { get; init; }
    }
}
