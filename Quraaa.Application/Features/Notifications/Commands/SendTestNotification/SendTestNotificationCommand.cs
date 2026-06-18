using MediatR;
using Quraaa.Application.Features.Notifications.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Notifications.Commands.SendTestNotification
{
    public record SendTestNotificationCommand(
        string DeviceToken,
        string? Title = null,
        string? Body = null,
        Dictionary<string, string>? Data = null
    ) : IRequest<AppResult<NotificationSendResponse>>;
}
