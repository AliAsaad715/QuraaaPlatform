namespace Quraaa.Application.Features.Authentication.Common
{
    public class IdentityResultDto
    {
        public bool Succeeded { get; private set; }
        public string? PasswordHash { get; private set; }
        public IEnumerable<string> Errors { get; private set; } = new List<string>();

        public static IdentityResultDto Success(string passwordHash) =>
            new() { Succeeded = true, PasswordHash = passwordHash };

        public static IdentityResultDto Failure(IEnumerable<string> errors) =>
            new() { Succeeded = false, Errors = errors };
    }
}
