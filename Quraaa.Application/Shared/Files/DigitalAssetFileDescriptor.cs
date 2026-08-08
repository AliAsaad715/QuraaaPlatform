namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// An approved, existing physical file ready to stream to an HTTP caller.
    /// ContentLength/ETag/LastModifiedUtc let the controller answer conditional
    /// (If-None-Match / If-Modified-Since) and range requests without touching
    /// the filesystem itself.
    /// </summary>
    public sealed record DigitalAssetFileDescriptor(
        string PhysicalPath,
        string DownloadFileName,
        string ContentType,
        long ContentLength,
        string ETag,
        DateTimeOffset LastModifiedUtc);
}
