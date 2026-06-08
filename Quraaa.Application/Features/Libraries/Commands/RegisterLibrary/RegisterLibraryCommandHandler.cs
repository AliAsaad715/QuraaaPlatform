using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Libraries.Commands.RegisterLibrary
{
    public class RegisterLibraryCommandHandler : BaseApplicationService<RegisterLibraryCommandHandler>, IRequestHandler<RegisterLibraryCommand, AppResult<LibraryResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IUserRepository _userRepository;

        public RegisterLibraryCommandHandler(
            ILibraryRepository libraryRepository,
            IUserRepository userRepository,
            ILogger<RegisterLibraryCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _userRepository = userRepository;
        }

        public async Task<AppResult<LibraryResponse>> Handle(RegisterLibraryCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<RegisterLibraryCommand, LibraryResponse>(request, async () =>
            {
                await _userRepository.GetUserByIdAsync(request.UserId);

                var library = new LibraryAggregate(
                    Guid.NewGuid(),
                    request.LibraryName,
                    request.Location,
                    request.LibraryImage,
                    request.HeaderImage,
                    request.Email,
                    request.UserId
                );

                await _libraryRepository.AddLibraryAsync(library);
                await _libraryRepository.SaveChangesAsync();

                return new LibraryResponse(
                    library.Id,
                    library.LibraryName,
                    library.Location,
                    library.LibraryImage,
                    library.HeaderImage,
                    library.Email,
                    library.UserId
                );
            }, "Library registered successfully");
        }
    }
}
