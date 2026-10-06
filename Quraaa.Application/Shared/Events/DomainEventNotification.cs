using MediatR;
using Quraaa.Domain.Shared.Events;

namespace Quraaa.Application.Shared.Events
{
    /// <summary>
    /// Carries a committed domain event to its MediatR handlers. The Domain project
    /// has no MediatR reference, so an event is wrapped here instead of implementing
    /// <see cref="INotification"/> itself: handle one with
    /// <c>INotificationHandler&lt;DomainEventNotification&lt;TDomainEvent&gt;&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Handlers run after the event's own transaction has committed, inside the
    /// domain event dispatcher's unit of work. Stage changes through repositories:
    /// the dispatcher commits them together with marking the events processed, so
    /// staged-only handlers take effect exactly once. Events saved together are
    /// handled together, in the order they were recorded. A handler with an outside
    /// effect can run again after a failed attempt, so it must be idempotent; the
    /// event's <see cref="IDomainEvent.EventId"/> identifies a replay.
    /// </remarks>
    public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
        where TDomainEvent : IDomainEvent;

    public static class DomainEventNotification
    {
        /// <summary>
        /// Wraps <paramref name="domainEvent"/> in the notification type of its
        /// runtime type, so MediatR resolves the handlers of that exact event.
        /// </summary>
        public static INotification For(IDomainEvent domainEvent)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            var notificationType = typeof(DomainEventNotification<>)
                .MakeGenericType(domainEvent.GetType());

            return (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
        }
    }
}
