using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quraaa.Application.Features.DomainEvents.Interfaces;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Persistence.Data;
using Quraaa.Persistence.Services;

namespace Quraaa.Persistence.Interceptors;

/// <summary>
/// Stores every domain event raised by tracked aggregates as an
/// <see cref="OutboxMessage"/> in the same save, so an event commits or rolls back
/// with the change that raised it. It knows nothing about individual event types:
/// what an event means is decided by its MediatR handlers, which the domain event
/// dispatcher runs after commit, and an event nobody handles is simply marked
/// processed.
/// </summary>
public sealed class DomainEventOutboxInterceptor : SaveChangesInterceptor
{
    private readonly IDomainEventDispatchSignal _dispatchSignal;

    // Scoped like the DbContext, so this only follows the saves of one unit of work.
    private bool _hasUnsavedOutboxMessages;

    public DomainEventOutboxInterceptor(IDomainEventDispatchSignal dispatchSignal)
    {
        _dispatchSignal = dispatchSignal;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        StageOutboxMessages(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        StageOutboxMessages(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        WakeDispatcher();
        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        WakeDispatcher();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void StageOutboxMessages(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var aggregatesWithEvents = context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        if (aggregatesWithEvents.Count == 0)
        {
            return;
        }

        // Events saved together share a correlation id, so the dispatcher hands
        // them to their handlers together: a bulk upload becomes one push.
        var correlationId = Guid.NewGuid();

        var outboxMessages = aggregatesWithEvents
            .SelectMany(aggregate => aggregate.DomainEvents)
            .Select(domainEvent => DomainEventSerializer.ToOutboxMessage(domainEvent, correlationId))
            .ToList();

        context.Set<OutboxMessage>().AddRange(outboxMessages);

        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        _hasUnsavedOutboxMessages = true;
    }

    // Inside an explicit transaction this fires before the commit; the dispatcher
    // then finds nothing yet, and its periodic scan picks the events up instead.
    private void WakeDispatcher()
    {
        if (!_hasUnsavedOutboxMessages)
        {
            return;
        }

        _hasUnsavedOutboxMessages = false;
        _dispatchSignal.RequestImmediateProcessing();
    }
}
