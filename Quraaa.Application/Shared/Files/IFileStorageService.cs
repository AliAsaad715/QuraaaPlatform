namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Low-level, path-traversal-safe file I/O against the private storage root
    /// (outside wwwroot — never reachable through static file middleware).
    /// Relative paths are always forward-slash, root-relative strings such as
    /// "books/pdf/{guid}.pdf"; callers never pass a physical path.
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>Saves a file under <paramref name="subFolder"/> and returns its relative path.</summary>
        Task<string> SaveAsync(
            IUploadedFile file,
            string subFolder,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves a stored relative path to its physical location, verifying it stays
        /// within the private root and currently exists on disk. Returns false — never
        /// throws — for a missing, invalid, or traversal-attempting path.
        /// </summary>
        bool TryGetPhysicalPath(string relativePath, out string physicalPath);

        /// <summary>Deletes a file by relative path. No-ops if it no longer exists.</summary>
        Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams every file under the private root without loading the tree into memory.
        /// Used by the retention cleanup worker to discover orphan candidates in bounded batches.
        /// </summary>
        IAsyncEnumerable<StoredFileEntry> EnumerateFilesAsync(
            CancellationToken cancellationToken = default);
    }
}
