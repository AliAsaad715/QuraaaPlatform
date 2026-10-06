using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Catalog
{
    public class BookAggregate : AggregateRoot
    {
        private readonly List<BookVersion> _recordedVersions = new();

        public string Title { get; private set; } = null!;
        public Guid? AuthorId { get; private set; }
        public string Description { get; private set; } = null!;
        public string CoverImageUrl { get; private set; } = null!;
        public Guid? CategoryId { get; private set; }
        public Language Language { get; private set; }
        public string? Isbn { get; private set; }

        // Master digital files for this book, captured once at the catalog level
        // (e.g. via bulk upload) so every merchant listing this book can reuse them
        // instead of each merchant re-uploading its own copy.
        public string? CanonicalPdfUrl { get; private set; }
        public string? CanonicalWordDocUrl { get; private set; }

        /// <summary>
        /// The version number of the details currently on this row. Every change
        /// increments it and records a matching <see cref="BookVersion"/>, so the
        /// book always has a restorable history.
        /// </summary>
        public int CurrentVersionNumber { get; private set; }

        /// <summary>
        /// The versions this instance recorded, plus any loaded alongside it. They
        /// are saved with the book. This is not the complete history; read that
        /// through the book version repository.
        /// </summary>
        public IReadOnlyCollection<BookVersion> RecordedVersions => _recordedVersions;

        /// <summary>Whether reports have taken this book out of the catalog.</summary>
        public BookModerationStatus ModerationStatus { get; private set; }

        public DateTime? HiddenAtUtc { get; private set; }

        /// <summary>Why moderation last changed this book's visibility.</summary>
        public string? ModerationNote { get; private set; }

        public bool IsPubliclyVisible =>
            ModerationStatus != BookModerationStatus.HiddenForReview;

        private BookAggregate() { }

        public BookAggregate(
            Guid id,
            string title,
            Guid? authorId,
            string description,
            string coverImageUrl,
            Language language,
            Guid? categoryId = null,
            string? isbn = null,
            string? canonicalPdfUrl = null,
            string? canonicalWordDocUrl = null)
        {
            Id = id;
            Title = title;
            AuthorId = authorId;
            Description = description;
            CoverImageUrl = coverImageUrl;
            CategoryId = categoryId;
            Language = language;
            Isbn = isbn;
            CanonicalPdfUrl = canonicalPdfUrl;
            CanonicalWordDocUrl = canonicalWordDocUrl;
            CurrentVersionNumber = 1;
            ModerationStatus = BookModerationStatus.Visible;
            RecordVersion(BookVersionReason.Created, LastModifiedBy);
        }

        public void UpdateDetails(
            string title,
            Guid? authorId,
            string description,
            string coverImageUrl,
            Guid categoryId,
            Language language,
            Guid modifiedBy)
        {
            ApplyDetails(
                title,
                authorId,
                description,
                coverImageUrl,
                categoryId,
                language,
                Isbn,
                modifiedBy);
        }

        /// <summary>
        /// Replaces the book's details, opens a new version and records its
        /// <see cref="BookVersion"/>, so the previous state stays restorable.
        /// </summary>
        /// <returns>The new <see cref="CurrentVersionNumber"/>.</returns>
        public int ApplyDetails(
            string title,
            Guid? authorId,
            string description,
            string coverImageUrl,
            Guid? categoryId,
            Language language,
            string? isbn,
            Guid modifiedBy)
        {
            ReplaceDetails(
                title,
                authorId,
                description,
                coverImageUrl,
                categoryId,
                language,
                isbn,
                modifiedBy);

            RecordVersion(BookVersionReason.Edited, modifiedBy);

            return CurrentVersionNumber;
        }

        /// <summary>
        /// Restores the details of an earlier version. The old content is copied
        /// forward as a new <see cref="BookVersionReason.Reverted"/> version, so
        /// history is never rewritten and the revert itself stays auditable.
        /// </summary>
        public void RevertTo(BookVersion target, string? moderationNote, Guid revertedBy)
        {
            ArgumentNullException.ThrowIfNull(target);

            if (target.BookId != Id)
            {
                throw new DomainException("The version belongs to a different book.");
            }

            if (target.VersionNumber >= CurrentVersionNumber)
            {
                throw new DomainException("Only an earlier version can be restored.");
            }

            ReplaceDetails(
                target.Title,
                target.AuthorId,
                target.Description,
                target.CoverImageUrl,
                target.CategoryId,
                target.Language,
                target.Isbn,
                revertedBy);

            RecordModerationNote(moderationNote, revertedBy);
            RecordVersion(BookVersionReason.Reverted, revertedBy, target.VersionNumber);
        }

        /// <summary>
        /// Records the snapshot of the current details when the stored history has
        /// no row for <see cref="CurrentVersionNumber"/>, as for a catalog row that
        /// predates version history. The caller checks the stored history first.
        /// </summary>
        public void RecordMissingCurrentVersion() =>
            RecordVersion(
                CurrentVersionNumber == 1 ? BookVersionReason.Created : BookVersionReason.Edited,
                LastModifiedBy);

        /// <summary>
        /// Marks the book as reported but still listable. Idempotent, and never
        /// downgrades a book that is already withheld.
        /// </summary>
        public void Flag(string? moderationNote)
        {
            if (ModerationStatus != BookModerationStatus.Visible)
            {
                return;
            }

            ModerationStatus = BookModerationStatus.Flagged;
            ModerationNote = Truncate(moderationNote);
            UpdateModificationTime();
        }

        /// <summary>
        /// Withholds the book from the catalog while moderators investigate.
        /// Reversible with <see cref="Restore"/>; no content is destroyed.
        /// </summary>
        public void HideForReview(string? moderationNote, Guid? hiddenBy = null)
        {
            if (ModerationStatus == BookModerationStatus.HiddenForReview)
            {
                return;
            }

            ModerationStatus = BookModerationStatus.HiddenForReview;
            HiddenAtUtc = DateTime.UtcNow;
            ModerationNote = Truncate(moderationNote);

            if (hiddenBy.HasValue)
            {
                UpdateAudit(hiddenBy.Value);
            }
            else
            {
                UpdateModificationTime();
            }
        }

        /// <summary>
        /// Records why moderation touched this book, without changing its
        /// visibility. Used by actions such as reverting a version.
        /// </summary>
        public void RecordModerationNote(string? moderationNote, Guid modifiedBy)
        {
            ModerationNote = Truncate(moderationNote);
            UpdateAudit(modifiedBy);
        }

        /// <summary>Returns a withheld or flagged book to the catalog.</summary>
        public void Restore(string? moderationNote, Guid restoredBy)
        {
            if (ModerationStatus == BookModerationStatus.Visible)
            {
                return;
            }

            ModerationStatus = BookModerationStatus.Visible;
            HiddenAtUtc = null;
            ModerationNote = Truncate(moderationNote);
            UpdateAudit(restoredBy);
        }

        private void ReplaceDetails(
            string title,
            Guid? authorId,
            string description,
            string coverImageUrl,
            Guid? categoryId,
            Language language,
            string? isbn,
            Guid modifiedBy)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainException("A book title is required.");
            }

            if (string.IsNullOrWhiteSpace(coverImageUrl))
            {
                throw new DomainException("A book cover image is required.");
            }

            Title = title.Trim();
            AuthorId = authorId;
            Description = description?.Trim() ?? string.Empty;
            CoverImageUrl = coverImageUrl.Trim();
            CategoryId = categoryId;
            Language = language;
            Isbn = string.IsNullOrWhiteSpace(isbn) ? null : isbn.Trim();
            CurrentVersionNumber++;
            UpdateAudit(modifiedBy);
        }

        private void RecordVersion(
            BookVersionReason reason,
            Guid? changedByUserId,
            int? revertedFromVersionNumber = null) =>
            _recordedVersions.Add(BookVersion.Capture(
                this,
                reason,
                changedByUserId,
                revertedFromVersionNumber));

        private static string? Truncate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= MaxModerationNoteLength
                ? trimmed
                : trimmed[..MaxModerationNoteLength];
        }

        public const int MaxModerationNoteLength = 500;
    }
}