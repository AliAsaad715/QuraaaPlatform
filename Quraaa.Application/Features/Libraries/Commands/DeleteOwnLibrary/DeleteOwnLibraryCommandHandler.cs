using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Libraries.Commands.DeleteOwnLibrary
{
    public class DeleteOwnLibraryCommandHandler
        : BaseApplicationService<DeleteOwnLibraryCommandHandler>,
          IRequestHandler<DeleteOwnLibraryCommand, AppResult>
    {
        private readonly ILibraryRepository _libraryRepository;
        private readonly ILibraryPasswordHasher _libraryPasswordHasher;
        private readonly IAdminModerationRepository _moderationRepository;

        public DeleteOwnLibraryCommandHandler(
            ILibraryRepository libraryRepository,
            ILibraryPasswordHasher libraryPasswordHasher,
            IAdminModerationRepository moderationRepository,
            ILogger<DeleteOwnLibraryCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _libraryRepository = libraryRepository;
            _libraryPasswordHasher = libraryPasswordHasher;
            _moderationRepository = moderationRepository;
        }

        public async Task<AppResult> Handle(
            DeleteOwnLibraryCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                if (!AccountDeletionConfirmation.Matches(request.ConfirmationPhrase))
                {
                    throw new ApplicationBusinessException(
                        AccountDeletionConfirmation.PhraseMismatchMessage,
                        nameof(DeleteOwnLibraryCommand.ConfirmationPhrase));
                }

                var library = await _libraryRepository.GetApprovedByUserIdAsync(
                    request.UserId,
                    cancellationToken)
                    ?? throw new NotFoundException("Library not found");

                if (!_libraryPasswordHasher.Verify(library.PasswordHash, request.Password))
                {
                    throw new ApplicationBusinessException(
                        AccountDeletionConfirmation.WrongPasswordMessage,
                        nameof(DeleteOwnLibraryCommand.Password));
                }

                var blockers = await _moderationRepository.GetLibraryDeletionBlockersAsync(
                    [library.Id],
                    cancellationToken);

                if (blockers.TryGetValue(library.Id, out var libraryBlockers)
                    && libraryBlockers.Count > 0)
                {
                    // Listings, payouts, and sold items all have to be settled
                    // first; deleting under them would orphan real history.
                    throw new ConflictException(
                        AdminModerationErrorCodes.StillReferenced
                        + " ("
                        + string.Join(", ", libraryBlockers.Select(blocker =>
                            $"{blocker.Reference}: {blocker.Count}"))
                        + ")");
                }

                var libraries = await _moderationRepository.GetLibrariesByIdsAsync(
                    [library.Id],
                    cancellationToken);

                _moderationRepository.RemoveLibraries(libraries);
                await _moderationRepository.SaveChangesAsync(cancellationToken);

                Logger.LogWarning(
                    "Library {LibraryId} was permanently deleted by its owner {UserId}.",
                    library.Id,
                    request.UserId);
            }, "Your library was permanently deleted");
        }
    }
}
