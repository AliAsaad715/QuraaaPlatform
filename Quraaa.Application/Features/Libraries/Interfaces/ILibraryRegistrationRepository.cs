using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryRegistrationRepository
    {
        Task<LibraryRegistrationSession?> GetSessionByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<LibraryRegistrationSession?> GetSessionByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default);

        Task<LibraryEmailVerificationChallenge?> GetChallengeByLibraryIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default);

        Task AddSessionAsync(
            LibraryRegistrationSession session,
            CancellationToken cancellationToken = default);

        Task AddChallengeAsync(
            LibraryEmailVerificationChallenge challenge,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
