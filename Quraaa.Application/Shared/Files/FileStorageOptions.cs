namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Binds to the "Storage" configuration section for legacy/seeded local files.
    /// </summary>
    public sealed class FileStorageOptions
    {
        /// <summary>
        /// Compatibility root for seeded and pre-Cloudinary book files. New uploads
        /// use durable external storage. A relative value is resolved against the
        /// host content root and is never exposed through static-file middleware.
        /// </summary>
        public string RootPath { get; set; } = "storage";
    }
}
