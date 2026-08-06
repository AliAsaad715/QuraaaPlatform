using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Otp.Exceptions;
using Quraaa.Application.Features.Otp.Interfaces;

namespace Quraaa.Infrastructure.Services
{
    public class FirebaseSmsGateway : IFirebaseSmsGateway
    {
        private const string OtpDeviceTokenConfigurationKey = "OTP_DEVICE_TOKEN";
        private static readonly TimeSpan GatewayMessageTimeToLive = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan OtpValidity = TimeSpan.FromMinutes(10);

        private readonly ILogger<FirebaseSmsGateway> _logger;
        private readonly IConfiguration _configuration;

        public FirebaseSmsGateway(
            ILogger<FirebaseSmsGateway> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendSmsRequestAsync(
            string phoneNumber,
            string otpCode,
            string purpose,
            string? smsGatewayDeviceToken = null,
            CancellationToken cancellationToken = default)
        {
            var dispatchStarted = false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var deviceToken = ResolveSmsGatewayDeviceToken(smsGatewayDeviceToken);
                var requestId = Guid.NewGuid().ToString("N");
                var issuedAtUtc = DateTimeOffset.UtcNow;
                var dispatchExpiresAtUtc = issuedAtUtc.Add(GatewayMessageTimeToLive);
                var otpExpiresAtUtc = issuedAtUtc.Add(OtpValidity);

                var message = new Message()
                {
                    Token = deviceToken,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        TimeToLive = GatewayMessageTimeToLive
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "action", "SEND_SMS" },
                        { "requestId", requestId },
                        { "purpose", purpose },
                        { "issuedAtUtc", issuedAtUtc.ToString("O") },
                        { "dispatchExpiresAtUtc", dispatchExpiresAtUtc.ToString("O") },
                        { "otpExpiresAtUtc", otpExpiresAtUtc.ToString("O") },
                        { "phoneNumber", phoneNumber },
                        { "otpCode", otpCode },
                        { "body", $"Your Quraaa platform verification code is: {otpCode}" }
                    }
                };

                var firebaseMessaging = FirebaseMessaging.DefaultInstance;
                cancellationToken.ThrowIfCancellationRequested();

                dispatchStarted = true;
                string response = await firebaseMessaging.SendAsync(message, cancellationToken);

                _logger.LogInformation(
                    "Successfully dispatched OTP request to SMS Gateway via FCM for {PhoneNumber}. Request ID: {RequestId}; Response ID: {Response}",
                    MaskPhoneNumber(phoneNumber),
                    requestId,
                    response);
            }
            catch (Exception exception)
            {
                var outcome = !dispatchStarted || IsDefiniteFirebaseRejection(exception)
                    ? SmsDispatchOutcome.DefinitelyNotDispatched
                    : SmsDispatchOutcome.Unknown;

                _logger.LogError(
                    exception,
                    "Failed to send OTP FCM message to gateway for {PhoneNumber}. Dispatch outcome: {DispatchOutcome}",
                    MaskPhoneNumber(phoneNumber),
                    outcome);

                throw new SmsDispatchException(
                    "Failed to communicate with the SMS gateway.",
                    outcome,
                    exception);
            }
        }

        private static bool IsDefiniteFirebaseRejection(Exception exception)
        {
            if (exception is ArgumentException)
            {
                return true;
            }

            return exception is FirebaseMessagingException firebaseException
                && firebaseException.MessagingErrorCode is
                    MessagingErrorCode.InvalidArgument
                    or MessagingErrorCode.Unregistered
                    or MessagingErrorCode.SenderIdMismatch;
        }

        private string ResolveSmsGatewayDeviceToken(string? smsGatewayDeviceToken)
        {
            var deviceToken = smsGatewayDeviceToken
                ?? _configuration[OtpDeviceTokenConfigurationKey]
                ?? Environment.GetEnvironmentVariable(OtpDeviceTokenConfigurationKey);

            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                throw new InvalidOperationException("OTP_DEVICE_TOKEN is not configured on the server.");
            }

            return deviceToken;
        }

        private static string MaskPhoneNumber(string phoneNumber)
        {
            if (phoneNumber.Length <= 4)
            {
                return "****";
            }

            return $"***{phoneNumber[^4..]}";
        }
    }
}
