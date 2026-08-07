using Quraaa.Application.Features.Files.Common;

namespace Quraaa.Application.Features.Files.Interfaces
{
    public interface IOrphanFileCandidateRepository
    {
        /// <summary>Of <paramref name="candidatePaths"/>, returns the ones already tracked (in any status).</summary>
        Task<HashSet<string>> GetTrackedRelativePathsAsync(
            IReadOnlyCollection<string> candidatePaths,
            CancellationToken cancellationToken = default);

        /// <summary>Starts tracking newly-detected orphan candidates as Pending.</summary>
        Task AddPendingAsync(
            IEnumerable<string> relativePaths,
            DateTime detectedAtUtc,
            CancellationToken cancellationToken = default);

        /// <summary>Pending candidates whose grace period has elapsed, oldest first.</summary>
        Task<IReadOnlyCollection<PendingOrphanFileCandidate>> GetPendingDueForDeletionAsync(
            DateTime cutoffUtc,
            int take,
            CancellationToken cancellationToken = default);

        /// <summary>Marks a candidate as hard-deleted. The row is kept for audit history.</summary>
        Task MarkDeletedAsync(Guid id, DateTime deletedAtUtc, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes tracking for a candidate that turned out to be referenced again
        /// before its grace period elapsed — its file was never touched.
        /// </summary>
        Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
