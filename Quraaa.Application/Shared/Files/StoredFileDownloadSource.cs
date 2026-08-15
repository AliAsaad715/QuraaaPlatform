namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Storage-provider result used to deliver an already-authorized private file.
    /// A source is either a legacy local path or a short-lived remote URI.
    /// </summary>
    public sealed record StoredFileDownloadSource(
        string FileExtension,
        string? PhysicalPath,
        Uri? RemoteDownloadUri,
        long? ContentLength,
        string? ETag,
        DateTimeOffset? LastModifiedUtc);
}
