namespace Quraaa.API.Requests.Authentication
{
    public class ResetForgotPasswordRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
