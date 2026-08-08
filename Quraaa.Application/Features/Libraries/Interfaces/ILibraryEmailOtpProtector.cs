namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryEmailOtpProtector
    {
        string DeriveCode(
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation);

        string HashCode(
            string otpCode,
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation);

        bool VerifyCode(
            string otpCode,
            Guid libraryId,
            Guid userId,
            string normalizedEmail,
            Guid generation,
            string expectedHash);
    }
}
