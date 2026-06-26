using MediatR;
using Microsoft.Extensions.Logging;
using IdentityServer.Helpers;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler : BaseApplicationService<LoginCommandHandler>, IRequestHandler<LoginCommand, AppResult<AuthResponse>>
    {
        private readonly IUserRepository _repo;
        private readonly IIdentityService _identityService;
        private readonly IPhoneService _phoneService;

        public LoginCommandHandler(
            ILogger<LoginCommandHandler> logger,
            IServiceProvider serviceProvider,
            IUserRepository repo,
            IIdentityService identityService,
            IPhoneService phoneService) : base(logger, serviceProvider)
        {
            _repo = repo;
            _identityService = identityService;
            _phoneService = phoneService;
        }

        public async Task<AppResult<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var formattedPhone = _phoneService.FormatToE164(request.PhoneNumber);
                if (string.IsNullOrEmpty(formattedPhone))
                {
                    throw new ApplicationBusinessException("Invalid phone number format.");
                }

                var signInResult = await _identityService.CheckPasswordAndGenerateTokensAsync(formattedPhone, request.Password);

                if (!signInResult.Succeeded)
                {
                    if (signInResult.FailureReason == SignInFailureReason.PhoneNumberNotConfirmed)
                    {
                        throw new ApplicationBusinessException("Phone number is not verified. Please complete registration.");
                    }

                    throw new ApplicationBusinessException("Invalid phone number or password.");
                }

                return signInResult.AuthResponse!;
            }, "User logged in successfully");
        }
    }
}
