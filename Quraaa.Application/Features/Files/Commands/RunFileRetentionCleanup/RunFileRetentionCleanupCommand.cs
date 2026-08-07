using MediatR;

namespace Quraaa.Application.Features.Files.Commands.RunFileRetentionCleanup
{
    /// <summary>
    /// Internal system command — not exposed over HTTP. Numeric settings are passed
    /// in by the caller (the retention BackgroundService) rather than read here via
    /// IOptions, so the Application layer stays free of hosting/config framework types.
    /// </summary>
    public sealed record RunFileRetentionCleanupCommand(
        int GraceDays,
        int DiscoveryBatchSize,
        int DeletionBatchSize,
        IReadOnlyCollection<string> ExcludedSubFolders) : IRequest<RunFileRetentionCleanupResult>;
}
