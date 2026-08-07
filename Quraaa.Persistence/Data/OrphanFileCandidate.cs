namespace Quraaa.Persistence.Data
{
    public enum OrphanFileCandidateStatus
    {
        Pending = 1,
        Deleted = 2,
    }

    /// <summary>
    /// Tracks a physical file under the private storage root that, as of
    /// <see cref="DetectedAtUtc"/>, was found on disk with no row referencing it
    /// (checked against Listings.DigitalAssetUrl and BookPurchases.PurchasedDigitalAssetUrl).
    /// Kept pending until the retention grace period elapses, then re-verified and
    /// hard-deleted — giving an in-flight upload or a rolled-back transaction time
    /// to attach a reference before the file is removed.
    /// </summary>
    public sealed class OrphanFileCandidate
    {
        private OrphanFileCandidate()
        {
        }

        public OrphanFileCandidate(string relativePath, DateTime detectedAtUtc)
        {
            Id = Guid.NewGuid();
            RelativePath = relativePath;
            DetectedAtUtc = detectedAtUtc;
            Status = OrphanFileCandidateStatus.Pending;
        }

        public Guid Id { get; private set; }
        public string RelativePath { get; private set; } = null!;
        public DateTime DetectedAtUtc { get; private set; }
        public OrphanFileCandidateStatus Status { get; private set; }
        public DateTime? DeletedAtUtc { get; private set; }
    }
}
