using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Notifications.Interfaces;

namespace Quraaa.Infrastructure.Services
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(ILogger<FirebaseNotificationService> logger)
        {
            _logger = logger;
        }

        public async Task<string> SendToDeviceAsync(
            string deviceToken,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var message = new Message
                {
                    Token = deviceToken,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data is null
                        ? null
                        : new Dictionary<string, string>(data)
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);

                _logger.LogInformation(
                    "Successfully sent push notification through FCM. Response ID: {Response}",
                    response);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send push notification through FCM.");
                throw new ApplicationException("Failed to send push notification through Firebase Cloud Messaging.", ex);
            }
        }
    }
}
