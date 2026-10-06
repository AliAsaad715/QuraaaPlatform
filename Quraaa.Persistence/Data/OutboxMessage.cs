namespace Quraaa.Persistence.Data
{
    /// <summary>
    /// One domain event, stored in the same transaction as the change that raised
    /// it and handed to its handlers after commit by the domain event dispatcher.
    /// Events stored by the same save share a <see cref="CorrelationId"/> and are
    /// handled together, in <see cref="OccurredAtUtc"/> order.
    /// </summary>
    public sealed class OutboxMessage
    {
        private OutboxMessage()
        {
        }

        public OutboxMessage(
            Guid id,
            string type,
            string payload,
            DateTime occurredAtUtc,
            Guid correlationId)
        {
            Id = id;
            Type = type;
            Payload = payload;
            OccurredAtUtc = occurredAtUtc;
            CorrelationId = correlationId;
            NextAttemptAtUtc = occurredAtUtc;
        }

        /// <summary>The event's own <c>EventId</c>.</summary>
        public Guid Id { get; private set; }

        /// <summary>The full name of the event's type in the Domain assembly.</summary>
        public string Type { get; private set; } = null!;

        /// <summary>The event serialized as JSON.</summary>
        public string Payload { get; private set; } = null!;

        public DateTime OccurredAtUtc { get; private set; }

        /// <summary>Shared by every event one save stored.</summary>
        public Guid CorrelationId { get; private set; }

        public int AttemptCount { get; private set; }

        public DateTime NextAttemptAtUtc { get; private set; }

        public DateTime? LeaseUntilUtc { get; private set; }

        public DateTime? ProcessedAtUtc { get; private set; }

        public DateTime? AbandonedAtUtc { get; private set; }

        public string? LastError { get; private set; }

        internal void Lease(DateTime leaseUntilUtc) => LeaseUntilUtc = leaseUntilUtc;

        internal void MarkProcessed(DateTime utcNow)
        {
            ProcessedAtUtc = utcNow;
            LeaseUntilUtc = null;
        }
    }
}
