namespace Quraaa.API.Requests.Otp
{
    public class SendOtpRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string SmsGatewayDeviceToken { get; set; } = string.Empty;
    }
}
