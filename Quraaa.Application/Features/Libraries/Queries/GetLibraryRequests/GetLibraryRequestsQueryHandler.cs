using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraryRequests
{
    public class GetLibraryRequestsQueryHandler
        : BaseApplicationService<GetLibraryRequestsQueryHandler>,
          IRequestHandler<GetLibraryRequestsQuery, AppResult<PagedResult<LibraryRequestResponse>>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public GetLibraryRequestsQueryHandler(
            ILibraryRepository libraryRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<GetLibraryRequestsQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<PagedResult<LibraryRequestResponse>>> Handle(
            GetLibraryRequestsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (items, totalCount) = await _libraryRepository.GetRequestsAsync(
                    request.Status, request.PageNumber, request.PageSize, request.SearchTerm, cancellationToken);

                var processedItems = items.Select(item => item with
                {
                    LibraryImage = _imageUrlFormatter.Format(item.LibraryImage),
                    HeaderImage = _imageUrlFormatter.Format(item.HeaderImage)
                }).ToList();

                return new PagedResult<LibraryRequestResponse>(
                    processedItems, request.PageNumber, request.PageSize, totalCount);

            }, "Library requests retrieved successfully.");
        }
    }
}