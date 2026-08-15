using Microsoft.AspNetCore.Identity;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Domain.Library;

namespace Quraaa.Persistence.Services
{
    /// <summary>
    /// Library dashboard passwords are hashed with ASP.NET Core Identity's
    /// PBKDF2 hasher — the same algorithm and work factor as account passwords.
    /// </summary>
    public sealed class LibraryPasswordHasher : ILibraryPasswordHasher
    {
        private readonly IPasswordHasher<LibraryAggregate> _passwordHasher;

        public LibraryPasswordHasher(IPasswordHasher<LibraryAggregate> passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("A password is required.", nameof(password));
            }

            return _passwordHasher.HashPassword(null!, password);
        }

        public bool Verify(string? passwordHash, string password)
        {
            if (string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            try
            {
                var result = _passwordHasher.VerifyHashedPassword(
                    null!,
                    passwordHash,
                    password);

                return result is PasswordVerificationResult.Success
                    or PasswordVerificationResult.SuccessRehashNeeded;
            }
            catch (FormatException)
            {
                // A stored value that is not a valid hash can never match.
                return false;
            }
        }
    }
}
