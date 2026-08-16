using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Admin.Common;
using Quraaa.Application.Features.Admin.Interfaces;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.User.Enums;

namespace Quraaa.Application.Features.Admin.Commands.CreateSuperAdmin
{
    public class CreateSuperAdminCommandHandler
        : BaseApplicationService<CreateSuperAdminCommandHandler>,
          IRequestHandler<CreateSuperAdminCommand, AppResult<AdminUserResponse>>
    {
        private readonly IAdminModerationRepository _moderationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationUnitOfWork _authenticationUnitOfWork;

        public CreateSuperAdminCommandHandler(
            IAdminModerationRepository moderationRepository,
            IUserRepository userRepository,
            IAuthenticationUnitOfWork authenticationUnitOfWork,
            ILogger<CreateSuperAdminCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _moderationRepository = moderationRepository;
            _userRepository = userRepository;
            _authenticationUnitOfWork = authenticationUnitOfWork;
        }

        public async Task<AppResult<AdminUserResponse>> Handle(
            CreateSuperAdminCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<CreateSuperAdminCommand, AdminUserResponse>(request, async () =>
            {
                // The caller's authority is re-checked here, not only at the
                // endpoint: this command hands out platform-wide power.
                var creator = await _userRepository.GetUserByIdAsync(request.CreatedByUserId)
                    ?? throw new NotFoundException("The creating account was not found.");

                if (creator.Role != Role.SuperAdmin)
                {
                    throw new UnauthorizedAccessException();
                }

                AdminUserResponse? created = null;
                await _authenticationUnitOfWork.ExecuteInTransactionAsync(
                    async transactionCancellationToken =>
                    {
                        // Identity's CreateAsync/AddToRoleAsync each save immediately.
                        // The shared DbContext transaction keeps those writes atomic
                        // with the matching platform profile save.
                        created = await _moderationRepository.CreateSuperAdminAsync(
                            request.PhoneNumber.Trim(),
                            request.Password,
                            request.FirstName.Trim(),
                            request.LastName.Trim(),
                            request.CreatedByUserId,
                            transactionCancellationToken);
                    },
                    cancellationToken);

                if (created is null)
                {
                    throw new InvalidOperationException(
                        "The super-admin creation transaction completed without a result.");
                }

                Logger.LogWarning(
                    "Super admin {CreatorId} created a new super admin {NewSuperAdminId}.",
                    request.CreatedByUserId,
                    created.UserId);

                return created;
            }, "Super admin created successfully");
        }
    }
}
