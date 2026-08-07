using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Files.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Files.Commands.RunFileRetentionCleanup
{
    public sealed class RunFileRetentionCleanupCommandHandler
        : IRequestHandler<RunFileRetentionCleanupCommand, RunFileRetentionCleanupResult>
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly IOrphanFileCandidateRepository _candidateRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IBookPurchaseRepository _purchaseRepository;
        private readonly ILogger<RunFileRetentionCleanupCommandHandler> _logger;

        public RunFileRetentionCleanupCommandHandler(
            IFileStorageService fileStorageService,
            IOrphanFileCandidateRepository candidateRepository,
            IListingRepository listingRepository,
            IBookPurchaseRepository purchaseRepository,
            ILogger<RunFileRetentionCleanupCommandHandler> logger)
        {
            _fileStorageService = fileStorageService;
            _candidateRepository = candidateRepository;
            _listingRepository = listingRepository;
            _purchaseRepository = purchaseRepository;
            _logger = logger;
        }

        public async Task<RunFileRetentionCleanupResult> Handle(
            RunFileRetentionCleanupCommand request,
            CancellationToken cancellationToken)
        {
            var nowUtc = DateTime.UtcNow;

            var (filesScanned, newCandidates) = await DiscoverAsync(request, nowUtc, cancellationToken);
            var (deleted, reprieved, failures) = await DeleteDueCandidatesAsync(request, nowUtc, cancellationToken);

            return new RunFileRetentionCleanupResult(filesScanned, newCandidates, deleted, reprieved, failures);
        }

        // ── Phase 1: discover new orphan candidates ─────────────────────────────
        // Streams the private root in bounded batches; each batch does exactly two
        // indexed "is this path referenced" lookups instead of loading every
        // Listing/BookPurchase row into memory.
        private async Task<(int FilesScanned, int NewCandidates)> DiscoverAsync(
            RunFileRetentionCleanupCommand request,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            var filesScanned = 0;
            var newCandidates = 0;
            var batch = new List<string>(request.DiscoveryBatchSize);

            await foreach (var entry in _fileStorageService.EnumerateFilesAsync(cancellationToken))
            {
                if (IsExcluded(entry.RelativePath, request.ExcludedSubFolders))
                    continue;

                filesScanned++;
                batch.Add(entry.RelativePath);

                if (batch.Count >= request.DiscoveryBatchSize)
                {
                    newCandidates += await ProcessDiscoveryBatchAsync(batch, nowUtc, cancellationToken);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                newCandidates += await ProcessDiscoveryBatchAsync(batch, nowUtc, cancellationToken);
            }

            return (filesScanned, newCandidates);
        }

        private async Task<int> ProcessDiscoveryBatchAsync(
            List<string> batch,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            var unreferenced = await FilterUnreferencedAsync(batch, cancellationToken);
            if (unreferenced.Count == 0)
                return 0;

            var alreadyTracked = await _candidateRepository.GetTrackedRelativePathsAsync(
                unreferenced, cancellationToken);
            var newPaths = unreferenced.Where(path => !alreadyTracked.Contains(path)).ToList();

            if (newPaths.Count == 0)
                return 0;

            await _candidateRepository.AddPendingAsync(newPaths, nowUtc, cancellationToken);
            await _candidateRepository.SaveChangesAsync(cancellationToken);

            return newPaths.Count;
        }

        // ── Phase 2: hard-delete candidates whose grace period has elapsed ──────
        private async Task<(int Deleted, int Reprieved, int Failures)> DeleteDueCandidatesAsync(
            RunFileRetentionCleanupCommand request,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            var cutoffUtc = nowUtc.AddDays(-request.GraceDays);
            var deleted = 0;
            var reprieved = 0;
            var failures = 0;

            while (true)
            {
                var due = await _candidateRepository.GetPendingDueForDeletionAsync(
                    cutoffUtc, request.DeletionBatchSize, cancellationToken);

                if (due.Count == 0)
                    break;

                // Defensive re-check: a listing update or new purchase may have
                // re-referenced one of these paths since it was first detected.
                var stillUnreferenced = await FilterUnreferencedAsync(
                    due.Select(candidate => candidate.RelativePath).ToList(), cancellationToken);

                foreach (var candidate in due)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!stillUnreferenced.Contains(candidate.RelativePath))
                    {
                        await _candidateRepository.RemoveAsync(candidate.Id, cancellationToken);
                        reprieved++;
                        continue;
                    }

                    try
                    {
                        // File first, DB second: if the process dies in between, the
                        // row simply stays Pending and the next pass finds the file
                        // already gone (File.Delete no-ops) and marks it deleted then.
                        await _fileStorageService.DeleteAsync(candidate.RelativePath, cancellationToken);
                        await _candidateRepository.MarkDeletedAsync(candidate.Id, nowUtc, cancellationToken);
                        deleted++;
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                    {
                        _logger.LogWarning(
                            exception,
                            "Failed to delete orphaned file {RelativePath}.",
                            candidate.RelativePath);
                        failures++;
                    }
                }

                if (due.Count < request.DeletionBatchSize)
                    break;
            }

            return (deleted, reprieved, failures);
        }

        private async Task<HashSet<string>> FilterUnreferencedAsync(
            IReadOnlyCollection<string> candidatePaths,
            CancellationToken cancellationToken)
        {
            var referencedByListings = await _listingRepository.FilterReferencedDigitalAssetPathsAsync(
                candidatePaths, cancellationToken);
            var referencedByPurchases = await _purchaseRepository.FilterReferencedDigitalAssetPathsAsync(
                candidatePaths, cancellationToken);

            return candidatePaths
                .Where(path => !referencedByListings.Contains(path) && !referencedByPurchases.Contains(path))
                .ToHashSet(StringComparer.Ordinal);
        }

        private static bool IsExcluded(string relativePath, IReadOnlyCollection<string> excludedSubFolders)
        {
            foreach (var excluded in excludedSubFolders)
            {
                if (relativePath.StartsWith(excluded.Trim('/') + "/", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
