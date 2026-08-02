using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Authentication.Commands.RefreshToken
{
    public class RefreshTokenCommandHandler : BaseApplicationService<RefreshTokenCommandHandler>, IRequestHandler<RefreshTokenCommand, AppResult<AuthResponse>>
    {
        private readonly IIdentityService _identityService;

        public RefreshTokenCommandHandler(
            IIdentityService identityService,
            ILogger<RefreshTokenCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _identityService = identityService;
        }

        public async Task<AppResult<AuthResponse>> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<RefreshTokenCommand, AuthResponse>(
                request,
                async () =>
                {
                    var authResponse = await _identityService
                        .RefreshAuthTokensAsync(
                            request.RefreshToken,
                            cancellationToken);

                    if (authResponse is null)
                    {
                        throw new UnauthenticatedException();
                    }

                    return authResponse;
                },
                "Authentication tokens refreshed successfully");
        }
    }
}
