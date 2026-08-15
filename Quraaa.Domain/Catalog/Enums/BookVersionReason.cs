namespace Quraaa.Domain.Catalog.Enums
{
    /// <summary>Why a book version was written.</summary>
    public enum BookVersionReason
    {
        /// <summary>The first version, captured when the book was created.</summary>
        Created = 1,

        /// <summary>An ordinary edit of the book's details.</summary>
        Edited = 2,

        /// <summary>
        /// A moderator restored the content of an earlier version. The earlier
        /// version is copied forward; history is never rewritten.
        /// </summary>
        Reverted = 3,
    }
}
