namespace Quraaa.Application.Features.Authentication.Common
{
    public static class AuthenticationPasswordPolicy
    {
        public const int MinimumLength = 6;
        public const int MaximumLength = 256;
        public const int RequiredUniqueCharacters = 1;
        public const bool RequireDigit = true;
        public const bool RequireLowercase = true;
        public const bool RequireUppercase = true;
        public const bool RequireNonAlphanumeric = true;

        public static bool MeetsComplexityRequirements(string? password)
        {
            return !string.IsNullOrEmpty(password)
                && (!RequireDigit || password.Any(IsAsciiDigit))
                && (!RequireLowercase || password.Any(IsAsciiLower))
                && (!RequireUppercase || password.Any(IsAsciiUpper))
                && (!RequireNonAlphanumeric || password.Any(character => !IsAsciiLetterOrDigit(character)));
        }

        // Match ASP.NET Identity's built-in PasswordValidator character classification.
        private static bool IsAsciiDigit(char character) =>
            character is >= '0' and <= '9';

        private static bool IsAsciiLower(char character) =>
            character is >= 'a' and <= 'z';

        private static bool IsAsciiUpper(char character) =>
            character is >= 'A' and <= 'Z';

        private static bool IsAsciiLetterOrDigit(char character) =>
            IsAsciiUpper(character) || IsAsciiLower(character) || IsAsciiDigit(character);
    }
}
