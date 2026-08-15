using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Catalog.Enums;

namespace Quraaa.Persistence.Interceptors
{
    /// <summary>
    /// Guarantees that a book's history is complete: every book gets version 1
    /// the moment it is created, and every change to its versioned details
    /// writes the matching snapshot — from any code path, including seeders and
    /// paths written later.
    ///
    /// This lives in an interceptor rather than at each call site because books
    /// are created in several places, and a forgotten call would silently break
    /// the one guarantee the history exists to provide.
    /// </summary>
    public sealed class BookVersionInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            CaptureVersions(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CaptureVersions(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static void CaptureVersions(DbContext? context)
        {
            if (context is null)
            {
                return;
            }

            var bookEntries = context.ChangeTracker
                .Entries<BookAggregate>()
                .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
                .ToList();

            if (bookEntries.Count == 0)
            {
                return;
            }

            // Versions a caller already staged itself (the revert path does), so
            // the same snapshot is never written twice.
            var stagedVersions = context.ChangeTracker
                .Entries<BookVersion>()
                .Where(entry => entry.State == EntityState.Added)
                .Select(entry => (entry.Entity.BookId, entry.Entity.VersionNumber))
                .ToHashSet();

            foreach (var entry in bookEntries)
            {
                var book = entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    if (stagedVersions.Contains((book.Id, book.CurrentVersionNumber)))
                    {
                        continue;
                    }

                    context.Add(BookVersion.Capture(
                        book,
                        BookVersionReason.Created,
                        book.LastModifiedBy));

                    continue;
                }

                // A modification only opens a version when the details actually
                // moved forward — moderation and audit-only updates do not.
                if (!HasNewVersionNumber(entry)
                    || stagedVersions.Contains((book.Id, book.CurrentVersionNumber)))
                {
                    continue;
                }

                context.Add(BookVersion.Capture(
                    book,
                    BookVersionReason.Edited,
                    book.LastModifiedBy));
            }
        }

        private static bool HasNewVersionNumber(EntityEntry<BookAggregate> entry)
        {
            var property = entry.Property(book => book.CurrentVersionNumber);

            return property.IsModified
                && property.CurrentValue > property.OriginalValue;
        }
    }
}
