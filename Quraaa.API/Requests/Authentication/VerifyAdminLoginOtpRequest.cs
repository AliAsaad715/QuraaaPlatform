namespace Quraaa.API.Requests.Authentication
{
    public class VerifyAdminLoginOtpRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
