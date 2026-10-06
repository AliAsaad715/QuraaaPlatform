using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.DomainEvents.Common;
using Quraaa.Application.Features.DomainEvents.Interfaces;
using Quraaa.Persistence.Configurations;
using Quraaa.Persistence.Data;
using Quraaa.Persistence.Services;

namespace Quraaa.Persistence.Repositories
{
    public sealed class OutboxMessageRepository : IOutboxMessageRepository
    {
        // Serializes claims across dispatchers and replicas. It must differ from
        // the demo-seeding lock (718568585).
        private const long ClaimLockKey = 718568586;

        private readonly ApplicationDbContext _context;

        public OutboxMessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<PendingDomainEvent>> ClaimNextBatchAsync(
            DateTime utcNow,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            if (leaseDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(leaseDuration),
                    "The outbox lease duration must be positive.");
            }

            var normalizedNow = NormalizeUtc(utcNow);

            await using var transaction = await _context.Database
                .BeginTransactionAsync(cancellationToken);

            // One claim at a time, so a save's events are always leased together
            // and never split between two dispatchers. The lock is released at
            // commit; handling the events happens after it.
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock({ClaimLockKey})",
                cancellationToken);

            var messages = await _context.OutboxMessages
                .FromSqlInterpolated($$"""
                    SELECT *
                    FROM "OutboxMessages"
                    WHERE "CorrelationId" = (
                            SELECT "CorrelationId"
                            FROM "OutboxMessages"
                            WHERE "ProcessedAtUtc" IS NULL
                              AND "AbandonedAtUtc" IS NULL
                              AND "NextAttemptAtUtc" <= {{normalizedNow}}
                              AND ("LeaseUntilUtc" IS NULL OR "LeaseUntilUtc" <= {{normalizedNow}})
                            ORDER BY "OccurredAtUtc", "Id"
                            LIMIT 1)
                      AND "ProcessedAtUtc" IS NULL
                      AND "AbandonedAtUtc" IS NULL
                    ORDER BY "OccurredAtUtc", "Id"
                    """)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                message.Lease(normalizedNow.Add(leaseDuration));
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return messages
                .Select(message => new PendingDomainEvent(
                    message.Id,
                    message.Type,
                    DomainEventSerializer.Deserialize(message.Type, message.Payload),
                    message.AttemptCount))
                .ToList();
        }

        public void MarkProcessed(IReadOnlyCollection<Guid> messageIds, DateTime utcNow)
        {
            var ids = messageIds.ToHashSet();
            var claimed = _context.OutboxMessages.Local
                .Where(message => ids.Contains(message.Id))
                .ToList();

            if (claimed.Count != ids.Count)
            {
                throw new InvalidOperationException(
                    "Only messages claimed in this unit of work can be marked processed.");
            }

            var normalizedNow = NormalizeUtc(utcNow);

            foreach (var message in claimed)
            {
                message.MarkProcessed(normalizedNow);
            }
        }

        public Task RecordFailureAsync(
            IReadOnlyCollection<Guid> messageIds,
            string error,
            DateTime? retryAtUtc,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            var ids = messageIds.ToArray();
            var normalizedNow = NormalizeUtc(utcNow);
            DateTime? abandonedAtUtc = retryAtUtc is null ? normalizedNow : null;
            var nextAttemptAtUtc = retryAtUtc is { } retryAt ? NormalizeUtc(retryAt) : normalizedNow;
            var lastError = error.Length <= OutboxMessageConfiguration.MaxLastErrorLength
                ? error
                : error[..OutboxMessageConfiguration.MaxLastErrorLength];

            // ExecuteUpdate commits straight away and leaves the change tracker
            // alone, so the failed handlers' staged changes are never saved with it.
            return _context.OutboxMessages
                .Where(message => ids.Contains(message.Id) && message.ProcessedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.AttemptCount, message => message.AttemptCount + 1)
                        .SetProperty(message => message.LastError, lastError)
                        .SetProperty(message => message.NextAttemptAtUtc, nextAttemptAtUtc)
                        .SetProperty(message => message.AbandonedAtUtc, abandonedAtUtc)
                        .SetProperty(message => message.LeaseUntilUtc, (DateTime?)null),
                    cancellationToken);
        }

        private static DateTime NormalizeUtc(DateTime value) =>
            value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
    }
}
