namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Prepares an already-authorized relative path for safe HTTP delivery.
    /// Callers (e.g. a purchase's download query) are responsible for deciding
    /// WHETHER the current user may access <paramref name="relativePath"/> —
    /// this service only concerns itself with HOW to serve it once approved.
    /// </summary>
    public interface IFileAccessService
    {
        /// <summary>
        /// Resolves <paramref name="relativePath"/> to an existing physical file and
        /// derives a download name from <paramref name="downloadFileNameStem"/> plus the
        /// asset's own extension. Returns false if the path is invalid, escapes the
        /// private storage root, or no longer exists on disk.
        /// </summary>
        bool TryPrepareDownload(
            string relativePath,
            string downloadFileNameStem,
            out DigitalAssetFileDescriptor descriptor);
    }
}
