namespace Quraaa.Application.Shared.Files
{
    /// <summary>One owned private file discovered in durable or legacy storage.</summary>
    public sealed record StoredFileEntry(
        string RelativePath,
        DateTime LastWriteTimeUtc,
        long LengthBytes);
}
