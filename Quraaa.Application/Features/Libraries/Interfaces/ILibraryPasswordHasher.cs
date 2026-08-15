namespace Quraaa.Application.Features.Libraries.Interfaces
{
    /// <summary>
    /// Hashes and verifies the library dashboard password, using the same
    /// algorithm ASP.NET Core Identity applies to account passwords.
    /// </summary>
    public interface ILibraryPasswordHasher
    {
        string Hash(string password);

        /// <summary>
        /// Whether the supplied password matches the stored hash. Returns
        /// <see langword="false"/> for a missing or unreadable hash rather than
        /// throwing, so callers treat it as a failed login.
        /// </summary>
        bool Verify(string? passwordHash, string password);
    }
}
