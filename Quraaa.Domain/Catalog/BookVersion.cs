using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Catalog
{
    /// <summary>
    /// An immutable snapshot of a book's details as they stood at one point in
    /// time. Books are never edited in place without leaving one of these
    /// behind, so any past state can be inspected and restored.
    /// </summary>
    public sealed class BookVersion : AggregateRoot
    {
        public Guid BookId { get; private set; }

        /// <summary>1 for the original, incrementing with every change.</summary>
        public int VersionNumber { get; private set; }

        public BookVersionReason Reason { get; private set; }

        /// <summary>
        /// The version this one restored, when <see cref="Reason"/> is
        /// <see cref="BookVersionReason.Reverted"/>.
        /// </summary>
        public int? RevertedFromVersionNumber { get; private set; }

        /// <summary>Who made the change, when the book records an actor.</summary>
        public Guid? ChangedByUserId { get; private set; }

        public string Title { get; private set; } = null!;
        public Guid? AuthorId { get; private set; }
        public string Description { get; private set; } = null!;
        public string CoverImageUrl { get; private set; } = null!;
        public Guid? CategoryId { get; private set; }
        public Language Language { get; private set; }
        public string? Isbn { get; private set; }

        private BookVersion() { }

        private BookVersion(
            Guid bookId,
            int versionNumber,
            BookVersionReason reason,
            int? revertedFromVersionNumber,
            Guid? changedByUserId,
            string title,
            Guid? authorId,
            string description,
            string coverImageUrl,
            Guid? categoryId,
            Language language,
            string? isbn)
        {
            if (bookId == Guid.Empty)
            {
                throw new DomainException("Book id is required for a book version.");
            }

            if (versionNumber < 1)
            {
                throw new DomainException("A book version number starts at 1.");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainException("A book version requires the book title.");
            }

            if (reason == BookVersionReason.Reverted && revertedFromVersionNumber is null or < 1)
            {
                throw new DomainException(
                    "A reverted version must record which version it restored.");
            }

            if (reason != BookVersionReason.Reverted && revertedFromVersionNumber is not null)
            {
                throw new DomainException(
                    "Only a reverted version may record a restored version number.");
            }

            Id = Guid.NewGuid();
            BookId = bookId;
            VersionNumber = versionNumber;
            Reason = reason;
            RevertedFromVersionNumber = revertedFromVersionNumber;
            ChangedByUserId = changedByUserId;
            Title = title.Trim();
            AuthorId = authorId;
            Description = description?.Trim() ?? string.Empty;
            CoverImageUrl = coverImageUrl?.Trim() ?? string.Empty;
            CategoryId = categoryId;
            Language = language;
            Isbn = string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim();
        }

        /// <summary>
        /// Captures the book's current details as the next version. The caller
        /// has already applied the change to the book.
        /// </summary>
        public static BookVersion Capture(
            BookAggregate book,
            BookVersionReason reason,
            Guid? changedByUserId,
            int? revertedFromVersionNumber = null)
        {
            ArgumentNullException.ThrowIfNull(book);

            return new BookVersion(
                book.Id,
                book.CurrentVersionNumber,
                reason,
                revertedFromVersionNumber,
                changedByUserId,
                book.Title,
                book.AuthorId,
                book.Description,
                book.CoverImageUrl,
                book.CategoryId,
                book.Language,
                book.Isbn);
        }
    }
}
