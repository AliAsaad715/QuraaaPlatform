namespace Quraaa.Application.Features.DomainEvents.Interfaces
{
    /// <summary>
    /// In-process wake-up channel between a save that stored domain events and the
    /// dispatcher that hands them to their handlers, so a push for a new listing
    /// does not wait for the next periodic scan. The scan remains the safety net:
    /// a missed signal only delays an event, it never loses one.
    /// </summary>
    public interface IDomainEventDispatchSignal
    {
        /// <summary>Requests an immediate dispatch pass.</summary>
        void RequestImmediateProcessing();

        /// <summary>
        /// Waits until <see cref="RequestImmediateProcessing"/> is called or
        /// <paramref name="timeout"/> elapses, whichever comes first. Returns
        /// <see langword="true"/> when woken by a signal.
        /// </summary>
        Task<bool> WaitForSignalAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }
}
