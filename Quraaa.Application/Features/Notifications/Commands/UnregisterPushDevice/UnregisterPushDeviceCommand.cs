using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Notifications.Commands.UnregisterPushDevice;

public sealed record UnregisterPushDeviceCommand(
    Guid UserId,
    string DeviceToken) : IRequest<AppResult>;
