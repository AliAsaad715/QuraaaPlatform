using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.Notifications.Enums;

namespace Quraaa.Application.Features.BookReports.Services
{
    /// <summary>
    /// Applies the platform's escalation ladder when a book is reported.
    /// Escalation is driven by the number of DISTINCT reporters, so one person
    /// filing repeatedly cannot move a book at all.
    ///
    /// 1 reporter  - the book is flagged and every library listing it is told.
    /// 2 reporters - administrators are pulled in alongside those libraries.
    /// 3 reporters - the book is withheld from the catalog pending review.
    ///
    /// Reverting a book to an earlier version is deliberately NOT automatic: it
    /// destroys current content on the say-so of unverified reports, so it stays
    /// an administrator action (see RevertBookToVersion).
    /// </summary>
    public sealed class BookReportEscalationService
    {
        /// <summary>Distinct reporters needed before the book is withheld.</summary>
        public const int HideThreshold = 3;

        /// <summary>Distinct reporters needed before administrators are told.</summary>
        public const int AdminThreshold = 2;

        private readonly IBookReportRepository _bookReportRepository;
        private readonly IBookVersionRepository _bookVersionRepository;
        private readonly IBookModerationNotificationRepository _notificationRepository;
        private readonly ILogger<BookReportEscalationService> _logger;

        public BookReportEscalationService(
            IBookReportRepository bookReportRepository,
            IBookVersionRepository bookVersionRepository,
            IBookModerationNotificationRepository notificationRepository,
            ILogger<BookReportEscalationService> logger)
        {
            _bookReportRepository = bookReportRepository;
            _bookVersionRepository = bookVersionRepository;
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        /// <summary>
        /// Escalates a freshly reported book. Stages the book's new moderation
        /// state and the recipients' notification rows; the caller commits them
        /// together with the report.
        /// </summary>
        /// <param name="bookId">The reported book.</param>
        /// <param name="reporterUserId">
        /// The reader whose report triggered this. Counted even though their
        /// report is still only staged, so the ladder does not lag by one.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task EscalateAsync(
            Guid bookId,
            Guid reporterUserId,
            CancellationToken cancellationToken)
        {
            var reporterCount = await _bookReportRepository.CountDistinctReportersAsync(
                bookId,
                reporterUserId,
                cancellationToken);

            if (reporterCount < 1)
            {
                return;
            }

            var book = await _bookVersionRepository.GetBookForUpdateAsync(bookId, cancellationToken);
            if (book is null)
            {
                _logger.LogWarning(
                    "Book {BookId} was reported but could not be loaded for escalation.",
                    bookId);
                return;
            }

            var level = ResolveLevel(reporterCount);

            ApplyBookState(book, level, reporterCount);

            // Past the top rung the state stops changing, so further reports
            // would otherwise re-send the same notice to everyone each time.
            if (reporterCount > HideThreshold)
            {
                _logger.LogInformation(
                    "Book {BookId} now has {ReporterCount} distinct reporter(s); already {Level}, so no further notices were queued.",
                    book.Id,
                    reporterCount,
                    level);
                return;
            }

            var recipients = await ResolveRecipientsAsync(bookId, level, cancellationToken);

            if (recipients.Count == 0)
            {
                return;
            }

            var utcNow = DateTime.UtcNow;

            var notifications = recipients
                .Select(recipient => BookModerationNotification.Create(
                    book.Id,
                    recipient.UserId,
                    recipient.Audience,
                    level,
                    reporterCount,
                    book.Title,
                    utcNow))
                .ToList();

            await _notificationRepository.AddRangeAsync(notifications, cancellationToken);

            _logger.LogInformation(
                "Book {BookId} escalated to {Level} on {ReporterCount} distinct reporter(s); {RecipientCount} notice(s) queued.",
                book.Id,
                level,
                reporterCount,
                notifications.Count);
        }

        /// <summary>
        /// Re-applies the ladder after a moderator changed a report's status.
        /// Rejecting reports lowers the distinct count, so a book auto-withheld
        /// by reports that were then dismissed comes back on its own instead of
        /// waiting for someone to remember the visibility endpoint.
        /// </summary>
        public async Task ReevaluateAsync(
            Guid bookId,
            Guid moderatorUserId,
            CancellationToken cancellationToken)
        {
            var book = await _bookVersionRepository.GetBookForUpdateAsync(bookId, cancellationToken);
            if (book is null)
            {
                return;
            }

            var reporterCount = await _bookReportRepository.CountDistinctReportersAsync(
                bookId,
                includingUserId: null,
                cancellationToken);

            if (reporterCount >= HideThreshold)
            {
                return;
            }

            if (book.ModerationStatus != BookModerationStatus.HiddenForReview)
            {
                return;
            }

            book.Restore(
                reporterCount == 0
                    ? "All reports against this book were dismissed."
                    : $"Reports dismissed; {reporterCount} open report(s) remain.",
                moderatorUserId);

            _logger.LogInformation(
                "Book {BookId} was returned to the catalogue by moderator {ModeratorId}; {ReporterCount} open reporter(s) remain.",
                book.Id,
                moderatorUserId,
                reporterCount);
        }

        private static BookModerationLevel ResolveLevel(int reporterCount) =>
            reporterCount >= HideThreshold
                ? BookModerationLevel.Hidden
                : reporterCount >= AdminThreshold
                    ? BookModerationLevel.Escalated
                    : BookModerationLevel.Flagged;

        private static void ApplyBookState(
            BookAggregate book,
            BookModerationLevel level,
            int reporterCount)
        {
            var note = $"Reported by {reporterCount} distinct user(s).";

            if (level == BookModerationLevel.Hidden)
            {
                // Reversible and no content is lost: an administrator restores
                // the book, or reverts it to an earlier version, after review.
                book.HideForReview(note);
                return;
            }

            book.Flag(note);
        }

        private async Task<IReadOnlyCollection<NotificationRecipient>> ResolveRecipientsAsync(
            Guid bookId,
            BookModerationLevel level,
            CancellationToken cancellationToken)
        {
            // Books are a shared catalogue with no single owner, so every
            // library currently listing the book is told.
            var libraryOwnerIds = await _bookReportRepository.GetListingLibraryOwnerIdsAsync(
                bookId,
                cancellationToken);

            var recipients = libraryOwnerIds
                .Select(userId => new NotificationRecipient(userId, BookModerationAudience.Library))
                .ToList();

            if (level == BookModerationLevel.Flagged)
            {
                return recipients;
            }

            var adminUserIds = await _bookReportRepository.GetAdminUserIdsAsync(cancellationToken);

            recipients.AddRange(adminUserIds
                .Select(userId => new NotificationRecipient(userId, BookModerationAudience.Admin)));

            // A person who is both an admin and a library owner is notified once,
            // as an admin.
            return recipients
                .GroupBy(recipient => recipient.UserId)
                .Select(group => group
                    .OrderByDescending(recipient => recipient.Audience == BookModerationAudience.Admin)
                    .First())
                .ToList();
        }

        private sealed record NotificationRecipient(Guid UserId, BookModerationAudience Audience);
    }
}
