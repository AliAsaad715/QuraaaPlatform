using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Notifications.Common;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;

namespace Quraaa.Application.Features.Notifications.Commands.SendTestNotification
{
    public class SendTestNotificationCommandHandler : BaseApplicationService<SendTestNotificationCommandHandler>, IRequestHandler<SendTestNotificationCommand, AppResult<NotificationSendResponse>>
    {
        private const string DefaultTitle = "Quraaa test notification";
        private const string DefaultBody = "Firebase Cloud Messaging is configured correctly.";

        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public SendTestNotificationCommandHandler(
            IFirebaseNotificationService firebaseNotificationService,
            ILogger<SendTestNotificationCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _firebaseNotificationService = firebaseNotificationService;
        }

        public async Task<AppResult<NotificationSendResponse>> Handle(SendTestNotificationCommand request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync<SendTestNotificationCommand, NotificationSendResponse>(request, async () =>
            {
                var data = request.Data is null
                    ? new Dictionary<string, string>()
                    : new Dictionary<string, string>(request.Data);

                data.TryAdd("type", "test_notification");
                data.TryAdd("sentBy", "quraaa_api");

                var messageId = await _firebaseNotificationService.SendToDeviceAsync(
                    request.DeviceToken,
                    string.IsNullOrWhiteSpace(request.Title) ? DefaultTitle : request.Title.Trim(),
                    string.IsNullOrWhiteSpace(request.Body) ? DefaultBody : request.Body.Trim(),
                    data,
                    cancellationToken);

                return new NotificationSendResponse(messageId);
            }, "Test notification sent successfully");
        }
    }
}
