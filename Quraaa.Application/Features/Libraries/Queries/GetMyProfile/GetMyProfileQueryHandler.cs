using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;
using System.Buffers.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
                var library = await _libraryRepository.GetByUserIdAsync(request.UserId, cancellationToken);
                if (library == null)
                    throw new NotFoundException("Library not found");

                return new MyProfileLibraryResponse
                (
                    library.LibraryName,
                    library.Location,
                    library.LibraryImage.StartsWith("http") ? library.LibraryImage : $"{_config["BaseAPIURL"]}/{library.LibraryImage.Replace("\\", "/")}",
                    library.HeaderImage.StartsWith("http") ? library.LibraryImage : $"{_config["BaseAPIURL"]}/{library.HeaderImage.Replace("\\", "/")}",
                    library.Email
                );
            }, "Library profile retrieved successfully");
        }
    }
}