namespace Quraaa.Application.Shared.Files
{
    /// <summary>
    /// Binds to the "Storage:FileRetention" configuration section.
    /// </summary>
    public sealed class FileRetentionOptions
    {
        /// <summary>
        /// Days an unreferenced file must remain unreferenced before it is hard-deleted.
        /// Protects against deleting a file whose owning row hasn't committed yet
        /// (e.g. an upload in progress) or a rollback that will re-attach it shortly.
        /// </summary>
        public int GraceDays { get; set; } = 21;

        /// <summary>Minutes between cleanup passes.</summary>
        public int ScanIntervalMinutes { get; set; } = 60;

        /// <summary>Files enumerated per discovery batch before checking the database.</summary>
        public int DiscoveryBatchSize { get; set; } = 500;

        /// <summary>Orphan candidates hard-deleted per pass.</summary>
        public int DeletionBatchSize { get; set; } = 200;

        /// <summary>
        /// Storage-reference prefixes skipped entirely by the scan. Normally empty:
        /// listing, book-canonical, and purchase snapshot references are all checked
        /// before deletion. Keep this only as an operational escape hatch.
        /// </summary>
        public string[] ExcludedSubFolders { get; set; } = [];
    }
}
