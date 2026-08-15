using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Notifications.Commands.RegisterPushDevice;

public sealed record RegisterPushDeviceCommand(
    Guid UserId,
    string DeviceToken) : IRequest<AppResult>;
