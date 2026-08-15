using Quraaa.Application.Features.Payouts.Interfaces;

namespace Quraaa.Application.Features.Payouts.Services
{
    /// <summary>
    /// Singleton, process-local implementation of
    /// <see cref="ISellerPayoutDispatchSignal"/>. Coalesces bursts: any number
    /// of requests raised while the processor is busy collapse into a single
    /// pending wake-up, and the processor's due-scan picks up everything
    /// committed by then.
    /// </summary>
    public sealed class SellerPayoutDispatchSignal : ISellerPayoutDispatchSignal
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
                // A wake-up is already pending; the next scan covers this
                // request too.
            }
        }

        public Task<bool> WaitForSignalAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return _pendingWakeUp.WaitAsync(timeout, cancellationToken);
        }
    }
}
