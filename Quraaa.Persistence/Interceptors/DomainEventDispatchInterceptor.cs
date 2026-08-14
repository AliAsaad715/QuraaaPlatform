using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Shared.Events;
using Quraaa.Domain.Shared.Entities;

namespace Quraaa.Persistence.Interceptors
{
    // Dispatches domain events queued on AggregateRoot instances only after
    // SaveChangesAsync has actually committed. Each event's handler is published
    // and swallowed independently so a notification failure (e.g. FCM being down)
    // can never surface as a failure of the request that triggered the save.
    public sealed class DomainEventDispatchInterceptor : SaveChangesInterceptor
    {
        private readonly IPublisher _publisher;
        private readonly ILogger<DomainEventDispatchInterceptor> _logger;

        public DomainEventDispatchInterceptor(
            IPublisher publisher,
            ILogger<DomainEventDispatchInterceptor> logger)
        {
            _publisher = publisher;
            _logger = logger;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is { } context)
            {
                await DispatchDomainEventsAsync(context, cancellationToken);
            }

            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        private async Task DispatchDomainEventsAsync(DbContext context, CancellationToken cancellationToken)
        {
            var aggregatesWithEvents = context.ChangeTracker.Entries<AggregateRoot>()
                .Select(entry => entry.Entity)
                .Where(aggregate => aggregate.DomainEvents.Count > 0)
                .ToList();

            if (aggregatesWithEvents.Count == 0)
            {
                return;
            }

            var domainEvents = aggregatesWithEvents.SelectMany(aggregate => aggregate.DomainEvents).ToList();

            foreach (var aggregate in aggregatesWithEvents)
            {
                aggregate.ClearDomainEvents();
            }

            foreach (var domainEvent in domainEvents)
            {
                try
                {
                    var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                    var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

                    await _publisher.Publish(notification, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to publish domain event {DomainEventType}. The originating request already committed and is unaffected.",
                        domainEvent.GetType().Name);
                }
            }
        }
    }
}
