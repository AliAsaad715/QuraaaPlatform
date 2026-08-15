namespace Quraaa.Domain.Reports.Enums
{
    /// <summary>
    /// The predefined grounds a reader may report a book on. Stored as int,
    /// serialized as its name.
    /// </summary>
    public enum BookReportReason
    {
        /// <summary>Content unsuitable for the platform (explicit, hateful, violent).</summary>
        InappropriateContent = 1,

        /// <summary>Wrong title, author, description, category, or language.</summary>
        IncorrectInformation = 2,

        /// <summary>Published without the rights holder's permission.</summary>
        CopyrightViolation = 3,

        /// <summary>Offensive or abusive language.</summary>
        OffensiveLanguage = 4,

        /// <summary>Spam, advertising, or deliberately misleading material.</summary>
        SpamOrMisleading = 5,

        /// <summary>Unreadable file, corrupted pages, or a bad scan.</summary>
        PoorQuality = 6,

        /// <summary>Anything else; the reporter must describe it.</summary>
        Other = 7,
    }
}
