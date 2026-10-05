using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Application.Features.FavoriteBooks.Common;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Payments.Exceptions;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Domain.Author;
using Quraaa.Domain.Cart;
using Quraaa.Domain.Cart.Entities;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Category;
using Quraaa.Domain.Favorites;
using Quraaa.Domain.Library;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Payouts;
using Quraaa.Domain.Reports;
using Quraaa.Domain.Reviews;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Entities;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Services
{
    /// <summary>
    /// The one place where a failed save becomes an application exception. These
    /// rules used to live in the SaveChangesAsync of whichever repository owned the
    /// table; their messages, error codes and change-tracker clean-up are unchanged.
    /// </summary>
    internal static class SaveChangesExceptionTranslator
    {
        private const string BookChangedMessage =
            "This book changed concurrently. Reload it and retry.";

        private const string PasswordResetChangedMessage =
            "The password reset changed concurrently. Retry the operation.";

        private const string DefaultConcurrencyMessage =
            "The data changed concurrently. Reload it and retry the operation.";

        // Optimistic-concurrency messages, picked by the first entity whose write failed.
        private static readonly IReadOnlyDictionary<Type, string> ConcurrencyMessages =
            new Dictionary<Type, string>
            {
                [typeof(OrderAggregate)] = "Checkout state changed concurrently. Retry the operation.",
                [typeof(OrderItem)] = "Checkout state changed concurrently. Retry the operation.",
                [typeof(PaymentAttempt)] = "Checkout state changed concurrently. Retry the operation.",
                [typeof(CartAggregate)] = "Cart changed concurrently. Reload it and retry the operation.",
                [typeof(CartItem)] = "Cart changed concurrently. Reload it and retry the operation.",
                [typeof(ListingAggregate)] = "Listing changed concurrently. Reload it and retry the operation.",
                [typeof(SellerPayoutAggregate)] = "The payout changed concurrently. Retry the operation.",
                [typeof(LibraryAggregate)] = "The library changed concurrently. Reload it and retry the operation.",
                [typeof(LibraryRegistrationSession)] = "The library registration changed concurrently. Reload it and retry.",
                [typeof(LibraryEmailVerificationChallenge)] = "The library registration changed concurrently. Reload it and retry.",
                [typeof(LibraryPasswordResetChallenge)] = PasswordResetChangedMessage,
                [typeof(UserAggregate)] = "The profile changed in another request. Reload it and try again.",
                [typeof(UserLocation)] = "The profile changed in another request. Reload it and try again.",
                [typeof(BookReportAggregate)] = "This report was updated by someone else. Reload it and retry.",
                [typeof(BookAggregate)] = BookChangedMessage,
                [typeof(BookVersion)] = BookChangedMessage,
            };

        /// <summary>
        /// Returns the exception to throw instead of <paramref name="exception"/>,
        /// the same instance when no rule applies, or null when the failure is a
        /// benign race and the save should count as complete.
        /// </summary>
        public static Exception? Translate(DbContext context, DbUpdateException exception)
        {
            if (exception is DbUpdateConcurrencyException)
            {
                return new ConflictException(ConcurrencyMessageFor(exception));
            }

            if (exception.InnerException is not PostgresException postgresException)
            {
                return exception;
            }

            return postgresException.SqlState switch
            {
                PostgresErrorCodes.UniqueViolation =>
                    TranslateUniqueViolation(context, exception, postgresException.ConstraintName),
                PostgresErrorCodes.ForeignKeyViolation =>
                    TranslateForeignKeyViolation(context, exception),
                _ => exception
            };
        }

        private static string ConcurrencyMessageFor(DbUpdateException exception)
        {
            foreach (var entry in exception.Entries)
            {
                if (ConcurrencyMessages.TryGetValue(entry.Entity.GetType(), out var message))
                {
                    return message;
                }
            }

            return DefaultConcurrencyMessage;
        }

        private static Exception? TranslateUniqueViolation(
            DbContext context,
            DbUpdateException exception,
            string? constraintName)
        {
            switch (constraintName)
            {
                case "IX_BookReports_UserId_BookId":
                    DetachFailed<BookReportAggregate>(exception);
                    return new ApplicationBusinessException(BookReportErrorCodes.DuplicateBookReport);

                case "IX_BookReviews_UserId_BookId":
                    DetachFailed<BookReviewAggregate>(exception);
                    return new ApplicationBusinessException(ReviewErrorCodes.DuplicateReview);

                case "IX_FavoriteBooks_UserId_BookId":
                    DetachFailed<FavoriteBookAggregate>(exception);
                    return new ApplicationBusinessException(FavoriteBookErrorCodes.DuplicateFavoriteBook);

                case "IX_Carts_UserId_Open":
                    DetachFailed<CartAggregate>(exception);
                    return new ConflictException(
                        "Another open cart was created concurrently. Reload your cart and retry the operation.");

                case "IX_Books_Isbn":
                case "IX_Books_Title_Author_Language_CI":
                    // Keep the context usable: the version interceptor staged one
                    // BookVersion per new book, and leaving them Added would break the
                    // next save with a foreign key to a book that was never inserted.
                    DetachAll<BookVersion>(context);
                    DetachAll<BookAggregate>(context);
                    return new ConflictException(constraintName == "IX_Books_Isbn"
                        ? "A book with this ISBN was catalogued concurrently. Retry the operation."
                        : "One or more books already exist with the same Title, Author, and Language combination.");

                case "IX_BookVersions_BookId_VersionNumber":
                    // Books carry no concurrency token, so two simultaneous edits or
                    // reverts collide on the version number instead. The loser retries.
                    return new ConflictException(BookChangedMessage);

                case "IX_Libraries_UserId":
                    return new ApplicationBusinessException(LibraryErrorCodes.DuplicateLibraryForUser);

                case "IX_Libraries_Email":
                    return new ApplicationBusinessException(LibraryErrorCodes.DuplicateLibraryEmail);

                case "IX_LibraryRegistrationSessions_UserId":
                case "IX_LibraryRegistrationSessions_TokenHash":
                case "IX_LibraryEmailVerificationChallenges_LibraryId":
                    return new ConflictException(
                        "A library registration session or email challenge already exists.");

                case "IX_LibraryPasswordResetChallenges_LibraryId":
                    // Both requests saw no challenge and inserted one. The winner's code
                    // was mailed; surface the same retryable conflict rather than a 500.
                    return new ConflictException(PasswordResetChangedMessage);

                case "IX_Orders_SourceCartId":
                    return new ConflictException(
                        "Another order is already being created from this cart. Reload it and retry.");

                case "IX_ProcessedPaymentEvents_Provider_EventId":
                    return new PaymentEventAlreadyProcessedException();

                case "IX_BookPurchases_OrderItemId":
                case "IX_SellerPayouts_OrderId_LibraryId":
                    // A verified webhook and provider reconciliation can both stage the
                    // same immutable purchases and seller-payout outbox rows before the
                    // order's concurrency update is issued. The losing transaction rolls
                    // back and retries against the now-paid order.
                    return new ConflictException(
                        "This order payment was finalized concurrently. Retry the operation.");

                case "IX_OrphanFileCandidates_RelativePath":
                    // Another instance started tracking the same path between our lookup
                    // and this insert; nothing is lost, so the save counts as done.
                    DetachAll<OrphanFileCandidate>(context);
                    return null;

                default:
                    return exception;
            }
        }

        private static Exception TranslateForeignKeyViolation(
            DbContext context,
            DbUpdateException exception)
        {
            if (RestoreDeleted<AuthorAggregate>(context))
            {
                return new ConflictException(
                    "This author cannot be deleted because one or more books still reference it.");
            }

            if (RestoreDeleted<CategoryAggregate>(context))
            {
                return new ConflictException(
                    "This category cannot be deleted because one or more books still reference it.");
            }

            return exception;
        }

        private static void DetachFailed<TEntity>(DbUpdateException exception)
        {
            foreach (var entry in exception.Entries.Where(entry => entry.Entity is TEntity))
            {
                entry.State = EntityState.Detached;
            }
        }

        private static void DetachAll<TEntity>(DbContext context) where TEntity : class
        {
            foreach (var entry in context.ChangeTracker.Entries<TEntity>().ToList())
            {
                entry.State = EntityState.Detached;
            }
        }

        // A refused delete leaves the entity tracked as it was, so the context stays usable.
        private static bool RestoreDeleted<TEntity>(DbContext context) where TEntity : class
        {
            var deleted = context.ChangeTracker
                .Entries<TEntity>()
                .Where(entry => entry.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in deleted)
            {
                entry.State = EntityState.Unchanged;
            }

            return deleted.Count > 0;
        }
    }
}
