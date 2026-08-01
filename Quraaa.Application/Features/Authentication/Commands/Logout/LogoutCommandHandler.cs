using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Authentication.Commands.Logout
{
    public class LogoutCommandHandler : BaseApplicationService<LogoutCommandHandler>, IRequestHandler<LogoutCommand, AppResult>
    {
        private readonly IIdentityService _identityService;
        private readonly IAccessTokenRevocationService _accessTokenRevocationService;

        public LogoutCommandHandler(
            IIdentityService identityService,
            IAccessTokenRevocationService accessTokenRevocationService,
            ILogger<LogoutCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _identityService = identityService;
            _accessTokenRevocationService = accessTokenRevocationService;
        }

        public async Task<AppResult> Handle(
            LogoutCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                await _identityService.RevokeRefreshTokenAsync(
                    request.RefreshToken,
                    cancellationToken);

                if (!string.IsNullOrWhiteSpace(request.AccessTokenId)
                    && request.AccessTokenExpiresAt.HasValue)
                {
                    await _accessTokenRevocationService.RevokeAsync(
                        request.AccessTokenId,
                        request.AccessTokenExpiresAt.Value,
                        cancellationToken);
                }
            }, "User logged out successfully");
        }
    }
}
