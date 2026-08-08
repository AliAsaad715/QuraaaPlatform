namespace Quraaa.Application.Features.Files.Common
{
    public sealed record PendingOrphanFileCandidate(
        Guid Id,
        string RelativePath,
        DateTime DetectedAtUtc);
}
