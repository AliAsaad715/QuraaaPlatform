namespace Quraaa.Application.Features.Payouts.Exceptions
{
    /// <summary>
    /// The provider reported that another request with the same idempotency
    /// key is still in flight (Stripe HTTP 409 idempotency_error): a competing
    /// process is already executing this payout attempt. The caller must back
    /// off without recording a failure — the other request's outcome is the
    /// attempt's outcome.
    /// </summary>
    public sealed class PayoutConcurrentAttemptException : Exception
    {
        public PayoutConcurrentAttemptException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
