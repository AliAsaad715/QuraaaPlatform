using MediatR;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.BookReports.Queries.GetBookReportReasons
{
    public record GetBookReportReasonsQuery
        : IRequest<AppResult<IReadOnlyCollection<BookReportReasonResponse>>>;
}
