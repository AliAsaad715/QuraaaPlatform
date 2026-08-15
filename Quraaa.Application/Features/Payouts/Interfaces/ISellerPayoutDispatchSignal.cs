namespace Quraaa.Application.Features.Payouts.Interfaces
{
    /// <summary>
    /// In-process wake-up channel between the code that commits new seller
    /// payouts (order payment finalization) and the background processor that
    /// transfers them. Lets a library owner receive their share seconds after
    /// an order is paid instead of waiting for the next periodic sweep.
    /// The periodic sweep remains the safety net, so a missed signal only
    /// delays a payout — it never loses one.
    /// </summary>
    public interface ISellerPayoutDispatchSignal
    {
        /// <summary>
        /// Requests an immediate processing pass. Call only AFTER the payouts
        /// have been committed, otherwise the processor may scan before the
        /// rows are visible.
        /// </summary>
        void RequestImmediateProcessing();

        /// <summary>
        /// Waits until <see cref="RequestImmediateProcessing"/> is called or
        /// <paramref name="timeout"/> elapses, whichever comes first. Returns
        /// <see langword="true"/> when woken by a signal.
        /// </summary>
        Task<bool> WaitForSignalAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }
}
