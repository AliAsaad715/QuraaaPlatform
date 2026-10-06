namespace Quraaa.Domain.Shared.Events
{
    /// <summary>
    /// A fact an aggregate records about a change to itself. The event is saved in
    /// the same transaction as that change and handled after the transaction
    /// commits, so no handler ever sees an event whose change was rolled back.
    /// </summary>
    public interface IDomainEvent
    {
        /// <summary>Identifies this occurrence, so handlers can recognise a replay.</summary>
        Guid EventId { get; }

        /// <summary>When the aggregate recorded the event, in UTC.</summary>
        DateTime OccurredAtUtc { get; }
    }
}
