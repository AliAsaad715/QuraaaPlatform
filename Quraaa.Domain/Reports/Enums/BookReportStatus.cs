namespace Quraaa.Domain.Reports.Enums
{
    /// <summary>
    /// Moderation state of a book report. Stored as int, serialized as its
    /// name.
    /// </summary>
    public enum BookReportStatus
    {
        /// <summary>Submitted and waiting for a moderator.</summary>
        Pending = 1,

        /// <summary>A moderator is investigating it.</summary>
        InReview = 2,

        /// <summary>Investigated and acted upon.</summary>
        Resolved = 3,

        /// <summary>Investigated and dismissed.</summary>
        Rejected = 4,
    }
}
