namespace Quraaa.Persistence.Data
{
    public sealed class ProcessedPaymentEvent
    {
        private ProcessedPaymentEvent()
        {
        }

        public ProcessedPaymentEvent(
            string provider,
            string eventId,
            string eventType,
            Guid? orderId,
            Guid? paymentAttemptId)
        {
            Id = Guid.NewGuid();
            Provider = provider;
            EventId = eventId;
            EventType = eventType;
            OrderId = orderId;
            PaymentAttemptId = paymentAttemptId;
            ProcessedAtUtc = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public string Provider { get; private set; } = null!;
        public string EventId { get; private set; } = null!;
        public string EventType { get; private set; } = null!;
        public Guid? OrderId { get; private set; }
        public Guid? PaymentAttemptId { get; private set; }
        public DateTime ProcessedAtUtc { get; private set; }
    }
}
