using Quraaa.Domain.Reports.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Reports
{
    /// <summary>
    /// A reader's report against a book. One report per reader per book; a
    /// moderator investigates it and records the outcome.
    /// </summary>
    public class BookReportAggregate : AggregateRoot
    {
        public const int MaxDetailsLength = 1000;
        public const int MaxModeratorNoteLength = 1000;

        public Guid UserId { get; private set; }
        public Guid BookId { get; private set; }
        public BookReportReason Reason { get; private set; }

        /// <summary>Reporter's own description. Required for <see cref="BookReportReason.Other"/>.</summary>
        public string? Details { get; private set; }

        public BookReportStatus Status { get; private set; }

        /// <summary>What the moderator found or decided.</summary>
        public string? ModeratorNote { get; private set; }

        public Guid? ReviewedByAdminId { get; private set; }
        public DateTime? ReviewedAtUtc { get; private set; }

        private BookReportAggregate() { }

        private BookReportAggregate(
            Guid id,
            Guid userId,
            Guid bookId,
            BookReportReason reason,
            string? details)
        {
            Id = id;
            UserId = userId;
            BookId = bookId;
            Reason = reason;
            Details = details;
            Status = BookReportStatus.Pending;
        }

        public static BookReportAggregate Create(
            Guid userId,
            Guid bookId,
            BookReportReason reason,
            string? details)
        {
            if (userId == Guid.Empty)
            {
                throw new DomainException("User id is required for a book report.");
            }

            if (bookId == Guid.Empty)
            {
                throw new DomainException("Book id is required for a book report.");
            }

            if (!Enum.IsDefined(reason))
            {
                throw new DomainException("The report reason is not a known reason.");
            }

            var normalizedDetails = Normalize(details, MaxDetailsLength, "Report details");

            // "Other" carries no meaning on its own, so the reporter has to say
            // what is wrong for a moderator to be able to act.
            if (reason == BookReportReason.Other && normalizedDetails is null)
            {
                throw new DomainException(
                    "Details are required when the report reason is Other.");
            }

            return new BookReportAggregate(
                Guid.NewGuid(),
                userId,
                bookId,
                reason,
                normalizedDetails);
        }

        /// <summary>
        /// Records a moderator's decision. Reports move forward only:
        /// Pending to InReview, and either of those to Resolved or Rejected.
        /// Re-applying the current status is a no-op so a double submit is
        /// harmless.
        /// </summary>
        public void Review(BookReportStatus status, Guid adminId, string? moderatorNote)
        {
            if (adminId == Guid.Empty)
            {
                throw new DomainException("An administrator id is required to review a report.");
            }

            if (!Enum.IsDefined(status))
            {
                throw new DomainException("The report status is not a known status.");
            }

            if (status == BookReportStatus.Pending)
            {
                throw new DomainException("A report cannot be moved back to Pending.");
            }

            if (IsClosed)
            {
                // Terminal states are final: a second moderator must not
                // silently overwrite the first one's decision.
                throw new ConflictException(
                    $"This report was already {Status} and cannot be changed.");
            }

            var normalizedNote = Normalize(moderatorNote, MaxModeratorNoteLength, "Moderator note");

            if (Status == status && normalizedNote is null)
            {
                return;
            }

            Status = status;
            ModeratorNote = normalizedNote ?? ModeratorNote;
            ReviewedByAdminId = adminId;
            ReviewedAtUtc = DateTime.UtcNow;
            UpdateAudit(adminId);
        }

        /// <summary>Whether the report has reached a terminal state.</summary>
        public bool IsClosed =>
            Status is BookReportStatus.Resolved or BookReportStatus.Rejected;

        private static string? Normalize(string? value, int maxLength, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();

            if (trimmed.Length > maxLength)
            {
                throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
            }

            return trimmed;
        }
    }
}
