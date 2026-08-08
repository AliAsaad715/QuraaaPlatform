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
        /// Relative sub-folders (forward-slash, root-relative) skipped entirely by the
        /// scan. Use this for assets that aren't linked to a persisted reference column
        /// yet — e.g. bulk-uploaded catalog PDFs/Word docs, which today are written to
        /// disk but never stored on <c>BookAggregate</c>, so this worker cannot tell
        /// whether they're still in use. Excluding them avoids deleting a whole
        /// feature's output; remove the exclusion once that linkage exists.
        /// </summary>
        public string[] ExcludedSubFolders { get; set; } = [];
    }
}
