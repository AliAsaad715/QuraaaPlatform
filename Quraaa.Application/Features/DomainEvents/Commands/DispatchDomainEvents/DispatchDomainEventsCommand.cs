using MediatR;

namespace Quraaa.Application.Features.DomainEvents.Commands.DispatchDomainEvents
{
    /// <summary>
    /// Hands the oldest ready batch of stored domain events to their handlers. It
    /// handles one batch per command, so a worker can give each batch a fresh scope.
    /// </summary>
    public sealed record DispatchDomainEventsCommand : IRequest<DispatchDomainEventsResult>;

    public sealed record DispatchDomainEventsResult(
        int EventCount,
        DomainEventBatchOutcome Outcome);

    public enum DomainEventBatchOutcome
    {
        /// <summary>No batch was ready.</summary>
        None = 0,

        /// <summary>Every handler ran and the batch is marked processed.</summary>
        Processed = 1,

        /// <summary>A handler failed; the batch will be tried again later.</summary>
        RetryScheduled = 2,

        /// <summary>A handler failed on the last allowed attempt; the batch was given up.</summary>
        Abandoned = 3,
    }
}
