namespace Quraaa.Application.Features.Files.Commands.RunFileRetentionCleanup
{
    public sealed record RunFileRetentionCleanupResult(
        int FilesScanned,
        int NewCandidatesDetected,
        int CandidatesDeleted,
        int CandidatesReprieved,
        int DeletionFailures);
}
