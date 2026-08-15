namespace Quraaa.Domain.Notifications.Enums
{
    /// <summary>
    /// How far a book's reports have escalated. Driven by the number of
    /// DISTINCT reporters, so one person cannot escalate alone.
    /// </summary>
    public enum BookModerationLevel
    {
        /// <summary>First report: the book is flagged and its sellers told.</summary>
        Flagged = 1,

        /// <summary>Second distinct reporter: administrators are pulled in.</summary>
        Escalated = 2,

        /// <summary>Third distinct reporter: the book is withheld pending review.</summary>
        Hidden = 3,
    }
}
