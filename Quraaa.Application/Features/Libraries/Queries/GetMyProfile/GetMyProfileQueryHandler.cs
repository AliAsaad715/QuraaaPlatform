using MediatR;
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
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public GetMyProfileQueryHandler(
            ILibraryRepository libraryRepository,
            IImageUrlFormatter imageUrlFormatter,
            ILogger<GetMyProfileQueryHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<AppResult<MyProfileLibraryResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(request.UserId, cancellationToken);
                if (library == null)
                    throw new NotFoundException("Library not found");

                return new MyProfileLibraryResponse
                (
                    library.LibraryName,
                    library.Location,
                    _imageUrlFormatter.Format(library.LibraryImage),
                    _imageUrlFormatter.Format(library.HeaderImage),
                    library.Email
                );
            }, "Library profile retrieved successfully");
        }
    }
}