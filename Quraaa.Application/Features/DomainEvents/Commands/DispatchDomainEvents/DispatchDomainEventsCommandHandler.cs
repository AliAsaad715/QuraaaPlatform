using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.DomainEvents.Common;
using Quraaa.Application.Features.DomainEvents.Interfaces;
using Quraaa.Application.Shared.Events;
using Quraaa.Application.Shared.Persistence;
using Quraaa.Domain.Shared.Events;

namespace Quraaa.Application.Features.DomainEvents.Commands.DispatchDomainEvents
{
    public sealed class DispatchDomainEventsCommandHandler
        : IRequestHandler<DispatchDomainEventsCommand, DispatchDomainEventsResult>
    {
        private const int MaximumAttempts = 8;
        private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan[] RetryDelays =
        [
            TimeSpan.FromSeconds(10),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(2),
            TimeSpan.FromHours(6),
            TimeSpan.FromHours(12)
        ];

        private readonly IOutboxMessageRepository _outboxMessageRepository;
        private readonly IPublisher _publisher;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DispatchDomainEventsCommandHandler> _logger;

        public DispatchDomainEventsCommandHandler(
            IOutboxMessageRepository outboxMessageRepository,
            IPublisher publisher,
            IUnitOfWork unitOfWork,
            ILogger<DispatchDomainEventsCommandHandler> logger)
        {
            _outboxMessageRepository = outboxMessageRepository;
            _publisher = publisher;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<DispatchDomainEventsResult> Handle(
            DispatchDomainEventsCommand request,
            CancellationToken cancellationToken)
        {
            var batch = await _outboxMessageRepository.ClaimNextBatchAsync(
                DateTime.UtcNow,
                LeaseDuration,
                cancellationToken);

            if (batch.Count == 0)
            {
                return new DispatchDomainEventsResult(0, DomainEventBatchOutcome.None);
            }

            var messageIds = batch.Select(message => message.MessageId).ToArray();

            try
            {
                // One unit of work for the whole batch: handlers can combine events
                // that were saved together, and whatever they stage commits with the
                // processed marks, so a retried batch never applies its effects twice.
                foreach (var message in batch)
                {
                    await _publisher.Publish(
                        DomainEventNotification.For(ReadEvent(message)),
                        cancellationToken);
                }

                _outboxMessageRepository.MarkProcessed(messageIds, DateTime.UtcNow);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new DispatchDomainEventsResult(batch.Count, DomainEventBatchOutcome.Processed);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Shutting down is not a failed attempt: the lease runs out and the
                // batch is handled again.
                throw;
            }
            catch (Exception exception)
            {
                return await RecordFailureAsync(batch, messageIds, exception);
            }
        }

        private async Task<DispatchDomainEventsResult> RecordFailureAsync(
            IReadOnlyList<PendingDomainEvent> batch,
            IReadOnlyCollection<Guid> messageIds,
            Exception exception)
        {
            var attempt = batch.Max(message => message.AttemptCount) + 1;
            var utcNow = DateTime.UtcNow;
            DateTime? retryAtUtc = attempt >= MaximumAttempts
                ? null
                : utcNow.Add(RetryDelays[Math.Min(attempt, RetryDelays.Length) - 1]);

            await _outboxMessageRepository.RecordFailureAsync(
                messageIds,
                $"{exception.GetType().Name}: {exception.Message}",
                retryAtUtc,
                utcNow,
                CancellationToken.None);

            if (retryAtUtc is null)
            {
                _logger.LogError(
                    exception,
                    "Gave up on {EventCount} domain event(s) ({EventTypes}) after {Attempt} attempts.",
                    batch.Count,
                    DescribeTypes(batch),
                    attempt);

                return new DispatchDomainEventsResult(batch.Count, DomainEventBatchOutcome.Abandoned);
            }

            _logger.LogWarning(
                exception,
                "Handling {EventCount} domain event(s) ({EventTypes}) failed on attempt {Attempt}; retrying at {RetryAtUtc:o}.",
                batch.Count,
                DescribeTypes(batch),
                attempt,
                retryAtUtc.Value);

            return new DispatchDomainEventsResult(batch.Count, DomainEventBatchOutcome.RetryScheduled);
        }

        private static IDomainEvent ReadEvent(PendingDomainEvent message) =>
            message.DomainEvent
                ?? throw new InvalidOperationException(
                    $"Outbox message {message.MessageId} holds an unreadable domain event of type '{message.EventType}'.");

        private static string DescribeTypes(IEnumerable<PendingDomainEvent> batch) =>
            string.Join(", ", batch.Select(message => message.EventType).Distinct(StringComparer.Ordinal));
    }
}
