using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.BookReports.Queries.GetBookReportById
{
    public class GetBookReportByIdQueryHandler
        : BaseApplicationService<GetBookReportByIdQueryHandler>,
          IRequestHandler<GetBookReportByIdQuery, AppResult<BookReportResponse>>
    {
        private readonly IBookReportRepository _bookReportRepository;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public GetBookReportByIdQueryHandler(
            IBookReportRepository bookReportRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<GetBookReportByIdQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookReportRepository = bookReportRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<BookReportResponse>> Handle(
            GetBookReportByIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetBookReportByIdQuery, BookReportResponse>(request, async () =>
            {
                var report = await _bookReportRepository.GetResponseByIdAsync(
                    request.ReportId,
                    cancellationToken);

                if (report is null)
                {
                    throw new NotFoundException("Book report was not found.");
                }

                return report with
                {
                    BookCoverImageUrl = _imageUrlFormatter.Format(report.BookCoverImageUrl),
                };
            }, "Book report retrieved successfully");
        }
    }
}
