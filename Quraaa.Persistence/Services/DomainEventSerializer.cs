using System.Text.Json;
using Quraaa.Domain.Shared.Events;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Services
{
    /// <summary>
    /// Converts domain events to and from <see cref="OutboxMessage"/> rows. A stored
    /// type name is only ever resolved to a concrete domain event type in the Domain
    /// assembly, never to an arbitrary type.
    /// </summary>
    internal static class DomainEventSerializer
    {
        private static readonly JsonSerializerOptions SerializerOptions =
            new(JsonSerializerDefaults.General);

        public static OutboxMessage ToOutboxMessage(IDomainEvent domainEvent, Guid correlationId)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);

            var eventType = domainEvent.GetType();

            return new OutboxMessage(
                domainEvent.EventId,
                eventType.FullName
                    ?? throw new InvalidOperationException("A domain event type must have a full name."),
                JsonSerializer.Serialize(domainEvent, eventType, SerializerOptions),
                ToUtc(domainEvent.OccurredAtUtc),
                correlationId);
        }

        /// <summary>
        /// Returns the stored event, or null when its type no longer exists or its
        /// payload no longer matches the type.
        /// </summary>
        public static IDomainEvent? Deserialize(string typeName, string payload)
        {
            var eventType = typeof(IDomainEvent).Assembly.GetType(typeName, throwOnError: false);

            if (eventType is null
                || eventType.IsAbstract
                || !typeof(IDomainEvent).IsAssignableFrom(eventType))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize(payload, eventType, SerializerOptions) as IDomainEvent;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static DateTime ToUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
    }
}
