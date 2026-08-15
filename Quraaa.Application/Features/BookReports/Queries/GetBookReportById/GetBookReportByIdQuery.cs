using MediatR;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.BookReports.Queries.GetBookReportById
{
    /// <summary>Administrator query: one report, for the review screen.</summary>
    public record GetBookReportByIdQuery(Guid ReportId)
        : IRequest<AppResult<BookReportResponse>>;
}
