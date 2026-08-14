using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : BaseApplicationService<ResetPasswordCommandHandler>, IRequestHandler<ResetPasswordCommand, AppResult>
    {
        private readonly IIdentityService _identityService;
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationUnitOfWork _authenticationUnitOfWork;

        public ResetPasswordCommandHandler(
            IIdentityService identityService,
            IUserRepository userRepository,
            IAuthenticationUnitOfWork authenticationUnitOfWork,
            ILogger<ResetPasswordCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _identityService = identityService;
            _userRepository = userRepository;
            _authenticationUnitOfWork = authenticationUnitOfWork;
        }

        public async Task<AppResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                await _authenticationUnitOfWork.ExecuteInTransactionAsync(
                    async transactionCancellationToken =>
                    {
                        var user = await _userRepository.GetUserByIdAsync(request.UserId);
                        if (user == null)
                        {
                            throw new NotFoundException("User was not found.");
                        }

                        var identityResult = await _identityService.ChangePasswordAsync(
                            request.UserId,
                            request.OldPassword,
                            request.NewPassword);

                        if (!identityResult.Succeeded)
                        {
                            var allErrors = string.Join(" | ", identityResult.Errors);
                            throw new ApplicationBusinessException(allErrors);
                        }

                        if (string.IsNullOrWhiteSpace(identityResult.PasswordHash))
                        {
                            throw new ApplicationBusinessException(
                                "Password was changed, but the updated password hash was not returned.");
                        }

                        user.UpdatePasswordHash(identityResult.PasswordHash!, request.UserId);
                        await _userRepository.SaveChangesAsync(transactionCancellationToken);
                    },
                    cancellationToken);
            }, "Password reset successfully");
        }
    }
}
