using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Admin.Commands.DeleteOwnSuperAdminAccount
{
    public class DeleteOwnSuperAdminAccountCommandHandler
        : BaseApplicationService<DeleteOwnSuperAdminAccountCommandHandler>,
          IRequestHandler<DeleteOwnSuperAdminAccountCommand, AppResult>
    {
        private readonly IAdminModerationRepository _moderationRepository;
        private readonly IIdentityService _identityService;

        public DeleteOwnSuperAdminAccountCommandHandler(
            IAdminModerationRepository moderationRepository,
            IIdentityService identityService,
            ILogger<DeleteOwnSuperAdminAccountCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
            _identityService = identityService;
        }

        public async Task<AppResult> Handle(
            DeleteOwnSuperAdminAccountCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                if (!AccountDeletionConfirmation.Matches(request.ConfirmationPhrase))
                {
                    throw new ApplicationBusinessException(
                        AccountDeletionConfirmation.PhraseMismatchMessage,
                        nameof(DeleteOwnSuperAdminAccountCommand.ConfirmationPhrase));
                }

                if (!await _identityService.CheckPasswordAsync(request.UserId, request.Password))
                {
                    throw new ApplicationBusinessException(
                        AccountDeletionConfirmation.WrongPasswordMessage,
                        nameof(DeleteOwnSuperAdminAccountCommand.Password));
                }

                var accounts = await _moderationRepository.GetUsersByIdsAsync(
                    [request.UserId],
                    cancellationToken);

                var account = accounts.FirstOrDefault()
                    ?? throw new NotFoundException("Your account was not found.");

                if (account.Role != Role.SuperAdmin)
                {
                    throw new UnauthorizedAccessException();
                }

                // Someone has to be able to administer the platform tomorrow.
                if (await _moderationRepository.CountSuperAdminsAsync(cancellationToken) <= 1)
                {
                    throw new ConflictException(AdminModerationErrorCodes.LastSuperAdmin);
                }

                var blockers = await _moderationRepository.GetUserDeletionBlockersAsync(
                    [request.UserId],
                    cancellationToken);

                if (blockers.TryGetValue(request.UserId, out var accountBlockers)
                    && accountBlockers.Count > 0)
                {
                    throw new ConflictException(
                        AdminModerationErrorCodes.StillReferenced
                        + " ("
                        + string.Join(", ", accountBlockers.Select(blocker =>
                            $"{blocker.Reference}: {blocker.Count}"))
                        + ")");
                }

                await _moderationRepository.RemoveUsersAsync([account], cancellationToken);
                await _moderationRepository.SaveChangesAsync(cancellationToken);

                Logger.LogWarning(
                    "Super admin {UserId} permanently deleted their own account.",
                    request.UserId);
            }, "Your account was permanently deleted");
        }
    }
}
