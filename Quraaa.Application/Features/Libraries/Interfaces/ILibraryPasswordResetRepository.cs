using Quraaa.Domain.Library;

namespace Quraaa.Application.Features.Libraries.Interfaces
{
    public interface ILibraryPasswordResetRepository
    {
        Task<LibraryPasswordResetChallenge?> GetByLibraryIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            LibraryPasswordResetChallenge challenge,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
