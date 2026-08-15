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
        /// Resolves <paramref name="storedReference"/> to an approved local or remote
        /// file source and derives a safe download name. Returns null if the reference
        /// is invalid, unowned, or no longer exists locally.
        /// </summary>
        Task<DigitalAssetFileDescriptor?> PrepareDownloadAsync(
            string storedReference,
            string downloadFileNameStem,
            CancellationToken cancellationToken = default);
    }
}
