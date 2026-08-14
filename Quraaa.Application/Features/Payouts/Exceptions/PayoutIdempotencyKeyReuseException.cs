namespace Quraaa.Application.Features.Payouts.Exceptions
{
    /// <summary>
    /// The provider rejected the request because its idempotency key was
    /// already used with different parameters (Stripe HTTP 400
    /// idempotency_error) — e.g. the destination wallet changed while an
    /// earlier attempt under the same key generation was unresolved. Waiting
    /// never fixes this: the caller must reconcile against the provider
    /// (adopt the earlier transfer if it exists, otherwise rotate the key).
    /// </summary>
    public sealed class PayoutIdempotencyKeyReuseException : Exception
    {
        public PayoutIdempotencyKeyReuseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
