using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Files.Common;
using Quraaa.Application.Features.Files.Interfaces;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public sealed class OrphanFileCandidateRepository : IOrphanFileCandidateRepository
    {
        private readonly ApplicationDbContext _context;

        public OrphanFileCandidateRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HashSet<string>> GetTrackedRelativePathsAsync(
            IReadOnlyCollection<string> candidatePaths,
            CancellationToken cancellationToken = default)
        {
            if (candidatePaths.Count == 0)
                return [];

            var tracked = await _context.OrphanFileCandidates
                .AsNoTracking()
                .Where(x => candidatePaths.Contains(x.RelativePath))
                .Select(x => x.RelativePath)
                .ToListAsync(cancellationToken);

            return tracked.ToHashSet(StringComparer.Ordinal);
        }

        public async Task AddPendingAsync(
            IEnumerable<string> relativePaths,
            DateTime detectedAtUtc,
            CancellationToken cancellationToken = default)
        {
            var candidates = relativePaths
                .Select(path => new OrphanFileCandidate(path, detectedAtUtc))
                .ToList();

            if (candidates.Count > 0)
            {
                await _context.OrphanFileCandidates.AddRangeAsync(candidates, cancellationToken);
            }
        }

        public async Task<IReadOnlyCollection<PendingOrphanFileCandidate>> GetPendingDueForDeletionAsync(
            DateTime cutoffUtc,
            int take,
            CancellationToken cancellationToken = default)
        {
            return await _context.OrphanFileCandidates
                .AsNoTracking()
                .Where(x => x.Status == OrphanFileCandidateStatus.Pending && x.DetectedAtUtc <= cutoffUtc)
                .OrderBy(x => x.DetectedAtUtc)
                .ThenBy(x => x.Id)
                .Take(take)
                .Select(x => new PendingOrphanFileCandidate(x.Id, x.RelativePath, x.DetectedAtUtc))
                .ToListAsync(cancellationToken);
        }

        // ExecuteUpdate/ExecuteDelete commit directly against the database without
        // loading the row into the change tracker — the caller has only an Id, so
        // there is nothing else worth fetching first.
        public Task MarkDeletedAsync(Guid id, DateTime deletedAtUtc, CancellationToken cancellationToken = default) =>
            _context.OrphanFileCandidates
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, OrphanFileCandidateStatus.Deleted)
                        .SetProperty(x => x.DeletedAtUtc, deletedAtUtc),
                    cancellationToken);

        public Task RemoveAsync(Guid id, CancellationToken cancellationToken = default) =>
            _context.OrphanFileCandidates
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                // A concurrently-running instance already started tracking one of
                // these paths between our lookup and this insert; treat it as a no-op.
                foreach (var entry in _context.ChangeTracker.Entries<OrphanFileCandidate>().ToList())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
