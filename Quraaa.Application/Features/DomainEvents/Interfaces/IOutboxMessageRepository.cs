using Quraaa.Application.Features.DomainEvents.Common;

namespace Quraaa.Application.Features.DomainEvents.Interfaces
{
    /// <summary>
    /// The durable queue of domain events. Every event is stored in the same
    /// transaction as the change that raised it, and stays queued until its
    /// handlers have run.
    /// </summary>
    public interface IOutboxMessageRepository
    {
        /// <summary>
        /// Leases the oldest ready batch: the events one save stored together, so
        /// their handlers see the whole batch. Commits the lease in its own short
        /// transaction and returns the events in the order they were recorded, or
        /// an empty list when nothing is ready.
        /// </summary>
        Task<IReadOnlyList<PendingDomainEvent>> ClaimNextBatchAsync(
            DateTime utcNow,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stages the claimed events as processed. The caller's unit of work commits
        /// this together with whatever the handlers staged.
        /// </summary>
        void MarkProcessed(IReadOnlyCollection<Guid> messageIds, DateTime utcNow);

        /// <summary>
        /// Records a failed attempt straight away, outside the unit of work, so it
        /// survives the handlers' discarded changes. The events become ready again
        /// at <paramref name="retryAtUtc"/>, or are abandoned when it is null.
        /// </summary>
        Task RecordFailureAsync(
            IReadOnlyCollection<Guid> messageIds,
            string error,
            DateTime? retryAtUtc,
            DateTime utcNow,
            CancellationToken cancellationToken = default);
    }
}
