using MediatR;
using Quraaa.Application.Features.Notifications.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Notifications.Commands.SendNotification
{
    public record SendNotificationCommand(
        Guid UserId,
        string DeviceToken,
        string Title,
        string Body,
        Dictionary<string, string>? Data = null
    ) : IRequest<AppResult<NotificationSendResponse>>;
}
