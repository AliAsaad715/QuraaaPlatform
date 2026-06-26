namespace Quraaa.Application.Features.Authentication.Common
{
    public enum SignInFailureReason
    {
        InvalidCredentials = 1,
        PhoneNumberNotConfirmed = 2
    }

    public class SignInResultDto
    {
        public bool Succeeded { get; private set; }
        public AuthResponse? AuthResponse { get; private set; }
        public SignInFailureReason? FailureReason { get; private set; }

        public static SignInResultDto Success(AuthResponse authResponse) =>
            new() { Succeeded = true, AuthResponse = authResponse };

        public static SignInResultDto Failure(SignInFailureReason failureReason) =>
            new() { Succeeded = false, FailureReason = failureReason };
    }
}
