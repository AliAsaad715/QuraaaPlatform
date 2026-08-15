using MediatR;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Domain.Reports.Enums;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.BookReports.Queries.GetLibraryBookReports
{
    /// <summary>
    /// Library owner query: every report filed against a book this library
    /// currently lists, newest first.
    /// </summary>
    public record GetLibraryBookReportsQuery : IRequest<AppResult<PagedResult<LibraryBookReportResponse>>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 20;

        /// <summary>Omit to see every status.</summary>
        public BookReportStatus? Status { get; init; }

        /// <summary>Narrow to a single one of the library's books.</summary>
        public Guid? BookId { get; init; }
    }
}
