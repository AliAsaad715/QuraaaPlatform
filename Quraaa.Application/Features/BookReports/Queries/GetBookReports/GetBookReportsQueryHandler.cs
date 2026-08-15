using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.BookReports.Queries.GetBookReports
{
    public class GetBookReportsQueryHandler
        : BaseApplicationService<GetBookReportsQueryHandler>,
          IRequestHandler<GetBookReportsQuery, AppResult<PagedResult<BookReportResponse>>>
    {
        private readonly IBookReportRepository _bookReportRepository;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public GetBookReportsQueryHandler(
            IBookReportRepository bookReportRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<GetBookReportsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookReportRepository = bookReportRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<PagedResult<BookReportResponse>>> Handle(
            GetBookReportsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetBookReportsQuery, PagedResult<BookReportResponse>>(request, async () =>
            {
                var (items, totalCount) = await _bookReportRepository.GetPagedAsync(
                    request.Status,
                    request.BookId,
                    request.ReporterUserId,
                    request.SearchTerm,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                var formattedItems = items
                    .Select(item => item with
                    {
                        BookCoverImageUrl = _imageUrlFormatter.Format(item.BookCoverImageUrl),
                    })
                    .ToList();

                return new PagedResult<BookReportResponse>(
                    formattedItems,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Book reports retrieved successfully");
        }
    }
}
