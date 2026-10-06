using Quraaa.Domain.Shared.Events;

namespace Quraaa.Application.Features.DomainEvents.Common
{
    /// <summary>
    /// A stored domain event leased for handling. <see cref="DomainEvent"/> is null
    /// when the stored <see cref="EventType"/> or payload can no longer be read.
    /// </summary>
    public sealed record PendingDomainEvent(
        Guid MessageId,
        string EventType,
        IDomainEvent? DomainEvent,
        int AttemptCount);
}
