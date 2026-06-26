namespace Quraaa.API.Requests.Authentication
{
    public class VerifyRegisterOtpRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }
}
