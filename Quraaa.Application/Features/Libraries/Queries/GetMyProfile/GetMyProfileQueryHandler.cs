using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Queries.GetMyProfile
{
    public class GetMyProfileQueryHandler : BaseApplicationService<GetMyProfileQueryHandler>, IRequestHandler<GetMyProfileQuery, AppResult<MyProfileLibraryResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IConfiguration _config;

        public GetMyProfileQueryHandler(
            ILibraryRepository libraryRepository,
            IConfiguration config,
            ILogger<GetMyProfileQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _config = config;
        }

        public async Task<AppResult<MyProfileLibraryResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(request.UserId, cancellationToken);
                if (library == null)
                    throw new NotFoundException("Library not found");

                var baseUrl = _config["BaseAPIURL"]?.TrimEnd('/');

                return new MyProfileLibraryResponse
                (
                    library.LibraryName,
                    library.Location,
                    FormatImageUrl(library.LibraryImage, baseUrl!),
                    FormatImageUrl(library.HeaderImage, baseUrl!),
                    library.Email
                );
            }, "Library profile retrieved successfully");
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