using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quraaa.Domain.Marketplace.Events;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Persistence.Interceptors;

/// <summary>
/// Converts listing domain events into durable outbox rows before the originating
/// SaveChanges operation is committed. Publication events created in the same save
/// are coalesced per library, so a bulk upload produces one push notification.
/// </summary>
public sealed class DomainEventOutboxInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is { } context)
        {
            AddOutboxMessages(context);
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is { } context)
        {
            AddOutboxMessages(context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void AddOutboxMessages(DbContext context)
    {
        var aggregatesWithEvents = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        if (aggregatesWithEvents.Count == 0)
        {
            return;
        }

        var domainEvents = aggregatesWithEvents
            .SelectMany(aggregate => aggregate.DomainEvents)
            .ToList();

        var unsupportedEventTypes = domainEvents
            .Where(domainEvent =>
                domainEvent is not LibraryListingPublishedDomainEvent
                    and not ListingDigitalAssetUpdatedDomainEvent)
            .Select(domainEvent => domainEvent.GetType().Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (unsupportedEventTypes.Length > 0)
        {
            throw new InvalidOperationException(
                $"No durable outbox mapping exists for domain event(s): {string.Join(", ", unsupportedEventTypes)}.");
        }

        var utcNow = DateTime.UtcNow;
        var outboxMessages = new List<ListingPushNotification>();

        var publicationGroups = domainEvents
            .OfType<LibraryListingPublishedDomainEvent>()
            .GroupBy(domainEvent => domainEvent.LibraryId);

        foreach (var group in publicationGroups)
        {
            var publications = group
                .GroupBy(domainEvent => domainEvent.ListingId)
                .Select(listingGroup => listingGroup.First())
                .ToArray();

            var singlePublication = publications.Length == 1
                ? publications[0]
                : null;

            outboxMessages.Add(ListingPushNotification.CreatePublication(
                group.Key,
                singlePublication?.BookId,
                singlePublication?.ListingId,
                publications.Length,
                utcNow));
        }

        var digitalAssetUpdates = domainEvents
            .OfType<ListingDigitalAssetUpdatedDomainEvent>()
            .GroupBy(domainEvent => domainEvent.ListingId)
            .Select(listingGroup => listingGroup.Last());

        foreach (var domainEvent in digitalAssetUpdates)
        {
            outboxMessages.Add(ListingPushNotification.CreateDigitalAssetUpdate(
                domainEvent.LibraryId,
                domainEvent.BookId,
                domainEvent.ListingId,
                utcNow));
        }

        context.Set<ListingPushNotification>().AddRange(outboxMessages);

        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }
    }
}
