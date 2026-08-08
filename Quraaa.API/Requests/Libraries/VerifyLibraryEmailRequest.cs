namespace Quraaa.API.Requests.Libraries
{
    public sealed class VerifyLibraryEmailRequest
    {
        public string Token { get; set; } = string.Empty;
        public Guid VerificationId { get; set; }
        public string OtpCode { get; set; } = string.Empty;
    }
}
