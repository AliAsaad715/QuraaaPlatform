namespace Quraaa.Application.Shared.Files
{
    /// <summary>One physical file discovered under the private storage root.</summary>
    public sealed record StoredFileEntry(
        string RelativePath,
        DateTime LastWriteTimeUtc,
        long LengthBytes);
}
