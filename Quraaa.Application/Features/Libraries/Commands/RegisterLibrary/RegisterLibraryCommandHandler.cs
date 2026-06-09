using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Library;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Commands.RegisterLibrary
{
    public class RegisterLibraryCommandHandler : BaseApplicationService<RegisterLibraryCommandHandler>, IRequestHandler<RegisterLibraryCommand, AppResult<LibraryResponse>>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILibraryImageStorageService _libraryImageStorageService;

        public RegisterLibraryCommandHandler(
            ILibraryRepository libraryRepository,
            IUserRepository userRepository,
            ILibraryImageStorageService libraryImageStorageService,
            ILogger<RegisterLibraryCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _userRepository = userRepository;
            _libraryImageStorageService = libraryImageStorageService;
        }

        public async Task<AppResult<LibraryResponse>> Handle(RegisterLibraryCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<RegisterLibraryCommand, LibraryResponse>(request, async () =>
            {
                var user = await _userRepository.GetUserByIdAsync(request.UserId);
                if (user == null)
                {
                    throw new NotFoundException("User was not found.");
                }

                string? libraryImagePath = null;
                string? headerImagePath = null;

                try
                {
                    libraryImagePath = await _libraryImageStorageService.SaveAsync(request.LibraryImage!, cancellationToken);
                    headerImagePath = await _libraryImageStorageService.SaveAsync(request.HeaderImage!, cancellationToken);

                    var library = new LibraryAggregate(
                        Guid.NewGuid(),
                        request.LibraryName,
                        request.Location,
                        libraryImagePath,
                        headerImagePath,
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
                        library.UserId,
                        library.ApprovalStatus
                    );
                }
                catch
                {
                    await DeleteStoredImagesAsync(libraryImagePath, headerImagePath, cancellationToken);
                    throw;
                }
            }, "Library registered successfully");
        }

        private async Task DeleteStoredImagesAsync(string? libraryImagePath, string? headerImagePath, CancellationToken cancellationToken)
        {
            try
            {
                await _libraryImageStorageService.DeleteAsync(libraryImagePath, cancellationToken);
                await _libraryImageStorageService.DeleteAsync(headerImagePath, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to delete uploaded library images after registration failure.");
            }
        }
    }
}
