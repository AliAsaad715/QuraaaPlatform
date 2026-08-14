namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// An approved private file ready for HTTP delivery. Exactly one of
    /// <see cref="PhysicalPath"/> and <see cref="RemoteDownloadUri"/> is populated.
    /// Remote URIs are short-lived and generated only after application authorization.
    /// </summary>
    public sealed record DigitalAssetFileDescriptor(
        string? PhysicalPath,
        Uri? RemoteDownloadUri,
        string DownloadFileName,
        string ContentType,
        long? ContentLength,
        string? ETag,
        DateTimeOffset? LastModifiedUtc);
}
