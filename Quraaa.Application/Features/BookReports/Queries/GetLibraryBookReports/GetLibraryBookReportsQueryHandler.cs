using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.BookReports.Queries.GetLibraryBookReports
{
    public class GetLibraryBookReportsQueryHandler
        : BaseApplicationService<GetLibraryBookReportsQueryHandler>,
          IRequestHandler<GetLibraryBookReportsQuery, AppResult<PagedResult<LibraryBookReportResponse>>>
    {
        private readonly IBookReportRepository _bookReportRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public GetLibraryBookReportsQueryHandler(
            IBookReportRepository bookReportRepository,
            ILibraryRepository libraryRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<GetLibraryBookReportsQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _bookReportRepository = bookReportRepository;
            _libraryRepository = libraryRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<PagedResult<LibraryBookReportResponse>>> Handle(
            GetLibraryBookReportsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<GetLibraryBookReportsQuery, PagedResult<LibraryBookReportResponse>>(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found");
                }

                var (items, totalCount) = await _bookReportRepository.GetPagedForLibraryAsync(
                    library.Id,
                    request.Status,
                    request.BookId,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                var formattedItems = items
                    .Select(item => item with
                    {
                        BookCoverImageUrl = _imageUrlFormatter.Format(item.BookCoverImageUrl),
                    })
                    .ToList();

                return new PagedResult<LibraryBookReportResponse>(
                    formattedItems,
                    request.PageNumber,
                    request.PageSize,
                    totalCount);
            }, "Reports on your books retrieved successfully");
        }
    }
}
