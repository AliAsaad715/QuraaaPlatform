using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Notifications.Commands.UnregisterPushDevice;

public sealed class UnregisterPushDeviceCommandHandler
    : BaseApplicationService<UnregisterPushDeviceCommandHandler>,
      IRequestHandler<UnregisterPushDeviceCommand, AppResult>
{
    private readonly IPushDeviceRepository _pushDeviceRepository;
    private readonly IUserRepository _userRepository;

    public UnregisterPushDeviceCommandHandler(
        IPushDeviceRepository pushDeviceRepository,
        IUserRepository userRepository,
        ILogger<UnregisterPushDeviceCommandHandler> logger,
        IServiceProvider serviceProvider)
        : base(logger, serviceProvider)
    {
        _pushDeviceRepository = pushDeviceRepository;
        _userRepository = userRepository;
    }

    public async Task<AppResult> Handle(
        UnregisterPushDeviceCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(request, async () =>
        {
            var user = await _userRepository.GetUserByIdAsync(request.UserId);
            if (user is null)
            {
                throw new NotFoundException("User was not found.");
            }

            await _pushDeviceRepository.UnregisterAsync(
                request.UserId,
                request.DeviceToken,
                cancellationToken);
        }, "Push notification device unregistered successfully.");
    }
}
