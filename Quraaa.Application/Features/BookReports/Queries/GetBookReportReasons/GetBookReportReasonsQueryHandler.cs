using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.BookReports.Queries.GetBookReportReasons
{
    public class GetBookReportReasonsQueryHandler
        : BaseApplicationService<GetBookReportReasonsQueryHandler>,
          IRequestHandler<GetBookReportReasonsQuery, AppResult<IReadOnlyCollection<BookReportReasonResponse>>>
    {
        public GetBookReportReasonsQueryHandler(
            ILogger<GetBookReportReasonsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
        }

        public Task<AppResult<IReadOnlyCollection<BookReportReasonResponse>>> Handle(
            GetBookReportReasonsQuery request,
            CancellationToken cancellationToken)
        {
            return ExecuteAsync<IReadOnlyCollection<BookReportReasonResponse>>(
                () => Task.FromResult<IReadOnlyCollection<BookReportReasonResponse>>(
                    BookReportReasonCatalog.GetAll()),
                "Report reasons retrieved successfully");
        }
    }
}
