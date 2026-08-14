using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Notifications.Commands.RegisterDeviceToken
{
    public class RegisterDeviceTokenCommandHandler
        : BaseApplicationService<RegisterDeviceTokenCommandHandler>,
          IRequestHandler<RegisterDeviceTokenCommand, AppResult>
    {
        private readonly IUserDeviceTokenRepository _userDeviceTokenRepository;

        public RegisterDeviceTokenCommandHandler(
            IUserDeviceTokenRepository userDeviceTokenRepository,
            ILogger<RegisterDeviceTokenCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _userDeviceTokenRepository = userDeviceTokenRepository;
        }

        public async Task<AppResult> Handle(RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                await _userDeviceTokenRepository.UpsertAsync(
                    request.RequestingUserId,
                    request.DeviceToken.Trim(),
                    DateTime.UtcNow,
                    cancellationToken);

                await _userDeviceTokenRepository.SaveChangesAsync(cancellationToken);

            }, "Device token registered successfully.");
        }
    }
}
