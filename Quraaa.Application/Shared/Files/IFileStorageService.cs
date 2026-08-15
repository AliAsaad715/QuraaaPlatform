namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Provider-neutral storage for private book files. New uploads are stored in
    /// durable external storage; legacy root-relative paths remain readable so
    /// existing seeded and pre-migration records continue to work.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file under <paramref name="subFolder"/> and returns an opaque,
        /// persistable storage reference. The reference is not a public download URL.
        /// </summary>
        Task<string> SaveAsync(
            IUploadedFile file,
            string subFolder,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens a seekable read stream for internal processing. The caller owns the
        /// returned stream. Returns null for a missing, invalid, or unowned reference.
        /// </summary>
        Task<Stream?> OpenReadAsync(
            string storedReference,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves a reference into either a legacy physical path or a short-lived
        /// remote download URI. The caller has already authorized access to the file.
        /// Returns null for a missing, invalid, or unowned reference.
        /// </summary>
        Task<StoredFileDownloadSource?> GetDownloadSourceAsync(
            string storedReference,
            string downloadFileName,
            CancellationToken cancellationToken = default);

        /// <summary>Deletes an owned file reference. No-ops if it no longer exists.</summary>
        Task DeleteAsync(string storedReference, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams owned private files across durable storage and the legacy local
        /// book root. Used by retention cleanup to discover orphan candidates.
        /// </summary>
        IAsyncEnumerable<StoredFileEntry> EnumerateFilesAsync(
            CancellationToken cancellationToken = default);
    }
}
