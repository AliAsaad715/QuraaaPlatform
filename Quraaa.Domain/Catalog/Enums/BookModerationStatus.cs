namespace Quraaa.Domain.Catalog.Enums
{
    /// <summary>
    /// Whether a book is publicly listable, under suspicion, or withheld while
    /// moderators investigate reports against it.
    /// </summary>
    public enum BookModerationStatus
    {
        /// <summary>Normal: publicly visible.</summary>
        Visible = 1,

        /// <summary>Reported at least once; still visible, but on the queue.</summary>
        Flagged = 2,

        /// <summary>
        /// Withheld from the catalog pending a moderator decision. Reversible —
        /// no data is destroyed.
        /// </summary>
        HiddenForReview = 3,
    }
}
