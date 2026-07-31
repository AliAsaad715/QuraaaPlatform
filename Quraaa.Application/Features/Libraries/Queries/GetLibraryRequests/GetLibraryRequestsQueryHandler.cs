using MediatR;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _config;

        public GetLibraryRequestsQueryHandler(
            ILibraryRepository libraryRepository,
            IConfiguration config,
            ILogger<GetLibraryRequestsQueryHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _config = config;
        }

        public async Task<AppResult<PagedResult<LibraryRequestResponse>>> Handle(
            GetLibraryRequestsQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (items, totalCount) = await _libraryRepository.GetRequestsAsync(
                    request.Status, request.PageNumber, request.PageSize, request.SearchTerm, cancellationToken);

                var baseUrl = _config["BaseAPIURL"]?.TrimEnd('/');

                var processedItems = items.Select(item => item with
                {
                    LibraryImage = FormatImageUrl(item.LibraryImage, baseUrl!),
                    HeaderImage = FormatImageUrl(item.HeaderImage, baseUrl!)
                }).ToList();

                return new PagedResult<LibraryRequestResponse>(
                    processedItems, request.PageNumber, request.PageSize, totalCount);

            }, "Library requests retrieved successfully.");
        }

        private string FormatImageUrl(string imagePath, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return string.Empty;

            if (imagePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                imagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return imagePath;
            }

            var cleanPath = imagePath.Replace("\\", "/").TrimStart('/');
            return $"{baseUrl}/{cleanPath}";
        }
    }
}