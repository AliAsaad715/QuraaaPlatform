using Quraaa.Application.Features.DomainEvents.Interfaces;

namespace Quraaa.Application.Features.DomainEvents.Services
{
    /// <summary>
    /// Singleton, process-local implementation of
    /// <see cref="IDomainEventDispatchSignal"/>. Coalesces bursts: any number of
    /// requests raised while the dispatcher is busy collapse into one pending
    /// wake-up, and its next pass picks up everything stored by then.
    /// </summary>
    public sealed class DomainEventDispatchSignal : IDomainEventDispatchSignal
    {
        // Max count 1: at most one pending wake-up is remembered.
        private readonly SemaphoreSlim _pendingWakeUp = new(0, 1);

        public void RequestImmediateProcessing()
        {
            try
            {
                _pendingWakeUp.Release();
            }
            catch (SemaphoreFullException)
            {
                // A wake-up is already pending; the next pass covers this request too.
            }
        }

        public Task<bool> WaitForSignalAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return _pendingWakeUp.WaitAsync(timeout, cancellationToken);
        }
    }
}
