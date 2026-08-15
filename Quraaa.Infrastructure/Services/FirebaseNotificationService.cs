using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Notifications.Common;
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

        public async Task<PushBatchDeliveryResult> SendToDevicesAsync(
            IReadOnlyCollection<string> deviceTokens,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            var tokens = deviceTokens
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .Select(token => token.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (tokens.Count == 0)
            {
                throw new ArgumentException(
                    "At least one device token is required.",
                    nameof(deviceTokens));
            }

            if (tokens.Count > 500)
            {
                throw new ArgumentException(
                    "Firebase multicast messages support at most 500 device tokens.",
                    nameof(deviceTokens));
            }

            try
            {
                var message = new MulticastMessage
                {
                    Tokens = tokens,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data is null
                        ? null
                        : new Dictionary<string, string>(data)
                };

                var response = await FirebaseMessaging.DefaultInstance
                    .SendEachForMulticastAsync(message, cancellationToken);

                var invalidTokens = new List<string>();
                var successfulTokens = new List<string>();
                var transientFailureTokens = new List<string>();

                for (var index = 0; index < response.Responses.Count; index++)
                {
                    var sendResponse = response.Responses[index];
                    if (sendResponse.IsSuccess)
                    {
                        successfulTokens.Add(tokens[index]);
                        continue;
                    }

                    if (IsPermanentlyInvalidToken(sendResponse.Exception))
                    {
                        invalidTokens.Add(tokens[index]);
                    }
                    else
                    {
                        transientFailureTokens.Add(tokens[index]);
                    }
                }

                _logger.LogInformation(
                    "FCM approval notification batch completed: {SuccessCount} succeeded, " +
                    "{InvalidTokenCount} invalid token(s), {TransientFailureCount} transient failure(s).",
                    response.SuccessCount,
                    invalidTokens.Count,
                    response.FailureCount - invalidTokens.Count);

                return new PushBatchDeliveryResult(
                    successfulTokens,
                    invalidTokens,
                    transientFailureTokens);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to send an FCM approval notification batch.");
                throw new ApplicationException(
                    "Failed to send push notifications through Firebase Cloud Messaging.",
                    exception);
            }
        }

        private static bool IsPermanentlyInvalidToken(
            FirebaseMessagingException? exception) =>
            exception?.MessagingErrorCode is MessagingErrorCode.Unregistered
                or MessagingErrorCode.SenderIdMismatch
                or MessagingErrorCode.InvalidArgument;
    }
}
