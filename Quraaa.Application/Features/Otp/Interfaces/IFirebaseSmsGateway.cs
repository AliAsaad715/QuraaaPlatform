namespace Quraaa.Application.Features.Otp.Interfaces
{
    public interface IFirebaseSmsGateway
    {
        Task SendSmsRequestAsync(
            string phoneNumber,
            string otpCode,
            string smsGatewayDeviceToken,
            CancellationToken cancellationToken = default);
    }
}
