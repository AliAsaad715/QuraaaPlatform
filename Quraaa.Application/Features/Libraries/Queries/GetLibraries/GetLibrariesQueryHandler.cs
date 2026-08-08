using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Libraries.Queries.GetLibraries
{
    public class GetLibrariesQueryHandler : BaseApplicationService<GetLibrariesQueryHandler>, IRequestHandler<GetLibrariesQuery, AppResult<PagedResult<PublicLibraryResponse>>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IConfiguration _config;

        public GetLibrariesQueryHandler(
            ILibraryRepository libraryRepository,
            IConfiguration config,
            ILogger<GetLibrariesQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _config = config;
        }

        public async Task<AppResult<PagedResult<PublicLibraryResponse>>> Handle(GetLibrariesQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var (libraries, totalCount) = await _libraryRepository.GetPagedAsync(
                    request.PageNumber,
                    request.PageSize,
                    request.SearchTerm,
                    cancellationToken);

                var baseUrl = _config["BaseAPIURL"]?.TrimEnd('/');

                var items = libraries
                    .Select(l => new PublicLibraryResponse(
                        l.Id,
                        l.LibraryName,
                        l.Location,
                        FormatImageUrl(l.LibraryImage, baseUrl!),
                        FormatImageUrl(l.HeaderImage, baseUrl!),
                        l.Email))
                    .ToList();

                return new PagedResult<PublicLibraryResponse>(items, request.PageNumber, request.PageSize, totalCount);
            }, "Libraries retrieved successfully");
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