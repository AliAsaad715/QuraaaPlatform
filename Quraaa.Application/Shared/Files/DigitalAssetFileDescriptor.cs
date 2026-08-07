namespace Quraaa.Application.Shared.Files
{
    /// <summary>An approved, existing physical file ready to stream to an HTTP caller.</summary>
    public sealed record DigitalAssetFileDescriptor(
        string PhysicalPath,
        string DownloadFileName,
        string ContentType);
}
