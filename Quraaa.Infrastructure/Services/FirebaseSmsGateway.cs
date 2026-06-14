using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Otp.Interfaces;

namespace Quraaa.Infrastructure.Services
{
    public class FirebaseSmsGateway : IFirebaseSmsGateway
    {
        private readonly ILogger<FirebaseSmsGateway> _logger;

        public FirebaseSmsGateway(ILogger<FirebaseSmsGateway> logger)
        {
            _logger = logger;
        }

        public async Task SendSmsRequestAsync(
            string phoneNumber,
            string otpCode,
            string smsGatewayDeviceToken,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var message = new Message()
                {
                    Token = smsGatewayDeviceToken,
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
