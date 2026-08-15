using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Commands.ChangeLibraryPassword
{
    public class ChangeLibraryPasswordCommandHandler
        : BaseApplicationService<ChangeLibraryPasswordCommandHandler>,
          IRequestHandler<ChangeLibraryPasswordCommand, AppResult>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILibraryPasswordHasher _libraryPasswordHasher;
        private readonly IIdentityService _identityService;

        public ChangeLibraryPasswordCommandHandler(
            ILibraryRepository libraryRepository,
            ILibraryPasswordHasher libraryPasswordHasher,
            IIdentityService identityService,
            ILogger<ChangeLibraryPasswordCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _libraryPasswordHasher = libraryPasswordHasher;
            _identityService = identityService;
        }

        public async Task<AppResult> Handle(
            ChangeLibraryPasswordCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.UserId,
                    cancellationToken);

                if (library is null)
                {
                    throw new NotFoundException("Library not found");
                }

                if (!_libraryPasswordHasher.Verify(library.PasswordHash, request.CurrentPassword))
                {
                    throw new ApplicationBusinessException(
                        "The current library password is incorrect.",
                        nameof(ChangeLibraryPasswordCommand.CurrentPassword));
                }

                // The dashboard must never be reachable with the owner's
                // personal account password.
                if (await _identityService.CheckPasswordAsync(request.UserId, request.NewPassword))
                {
                    throw new ApplicationBusinessException(
                        LibraryPasswordRules.MustDifferFromAccountPasswordMessage,
                        nameof(ChangeLibraryPasswordCommand.NewPassword));
                }

                library.SetPasswordHash(
                    _libraryPasswordHasher.Hash(request.NewPassword),
                    request.UserId);

                await _libraryRepository.SaveChangesAsync();

                // Mirrors account password changes: replacing the credential
                // ends every session it could have opened, including the
                // caller's own.
                await _identityService.RevokeActiveSessionsAsync(request.UserId);

                Logger.LogInformation(
                    "Library {LibraryId} dashboard password was changed by its owner.",
                    library.Id);
            }, "Library password changed successfully");
        }
    }
}
