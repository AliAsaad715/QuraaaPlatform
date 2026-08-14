using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Notifications.Common;
using Quraaa.Application.Features.Notifications.Interfaces;

namespace Quraaa.Infrastructure.Services
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        // FCM/FirebaseAdmin-enforced hard limit on tokens per MulticastMessage.
        private const int MaxTokensPerMulticastRequest = 500;

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

        public async Task<FirebaseMulticastResult> SendMulticastAsync(
            IReadOnlyCollection<string> deviceTokens,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            if (deviceTokens.Count == 0)
            {
                return new FirebaseMulticastResult(0, 0, []);
            }

            var successCount = 0;
            var failureCount = 0;
            var invalidTokens = new List<string>();

            foreach (var chunk in deviceTokens.Chunk(MaxTokensPerMulticastRequest))
            {
                var message = new MulticastMessage
                {
                    Tokens = chunk,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data is null
                        ? null
                        : new Dictionary<string, string>(data)
                };

                try
                {
                    var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);

                    successCount += response.SuccessCount;
                    failureCount += response.FailureCount;

                    for (var i = 0; i < response.Responses.Count; i++)
                    {
                        var sendResponse = response.Responses[i];
                        if (sendResponse.IsSuccess)
                        {
                            continue;
                        }

                        _logger.LogWarning(
                            sendResponse.Exception,
                            "Failed to deliver a multicast push notification to one device token.");

                        if (sendResponse.Exception?.MessagingErrorCode is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument)
                        {
                            invalidTokens.Add(chunk[i]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    failureCount += chunk.Length;
                    _logger.LogError(ex, "Multicast push notification batch failed for {Count} device tokens.", chunk.Length);
                }
            }

            return new FirebaseMulticastResult(successCount, failureCount, invalidTokens);
        }
    }
}
