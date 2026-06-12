using MediatR;
using Microsoft.Extensions.Logging;
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

        public LoginCommandHandler(
            ILogger<LoginCommandHandler> logger,
            IServiceProvider serviceProvider,
            IUserRepository repo,
            IIdentityService identityService) : base(logger, serviceProvider)
        {
            _repo = repo;
            _identityService = identityService;
        }

        public async Task<AppResult<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var authResponse = await _identityService.CheckPasswordAndGenerateTokensAsync(request.PhoneNumber, request.Password);

                if (authResponse == null)
                {
                    throw new ApplicationBusinessException("Invalid phone number or password.");
                }

                return authResponse;
            }, "User logged in successfully");
        }
    }
}
