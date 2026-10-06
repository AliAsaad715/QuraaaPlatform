namespace Quraaa.Domain.Shared.Events
{
    /// <summary>
    /// Base record for domain events. Stamps each new event with its own
    /// <see cref="EventId"/> and the time it was recorded; a stored event gets both
    /// back through the init setters when it is read again.
    /// </summary>
    public abstract record DomainEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();

        public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    }
}
