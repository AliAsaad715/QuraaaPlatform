using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Otp.Interfaces;

namespace Quraaa.Infrastructure.Services
{
    public class FirebaseSmsGateway : IFirebaseSmsGateway
    {
        private const string OtpDeviceTokenConfigurationKey = "OTP_DEVICE_TOKEN";

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
            string? smsGatewayDeviceToken = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var deviceToken = ResolveSmsGatewayDeviceToken(smsGatewayDeviceToken);

                var message = new Message()
                {
                    Token = deviceToken,
                    Data = new Dictionary<string, string>()
                    {
                        { "action", "SEND_SMS" },
                        { "phoneNumber", phoneNumber },
                        { "otpCode", otpCode },
                        { "body", $"Your Quraaa platform verification code is: {otpCode}" }
                    }
                };

                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message, cancellationToken);

                _logger.LogInformation(
                    "Successfully dispatched OTP request to SMS Gateway via FCM for {PhoneNumber}. Response ID: {Response}",
                    MaskPhoneNumber(phoneNumber),
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP FCM message to gateway for {PhoneNumber}", MaskPhoneNumber(phoneNumber));
                throw new ApplicationException("Failed to communicate with the SMS gateway.", ex);
            }
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
