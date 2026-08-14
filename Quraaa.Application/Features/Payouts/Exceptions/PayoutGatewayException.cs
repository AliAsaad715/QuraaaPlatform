namespace Quraaa.Application.Features.Payouts.Exceptions
{
    /// <summary>
    /// Represents a payout-provider call that failed. The payout processor
    /// records the failure and retries with backoff; wallet validation maps it
    /// to an HTTP 503.
    /// </summary>
    public sealed class PayoutGatewayException : Exception
    {
        public PayoutGatewayException(string message)
            : base(message)
        {
        }

        public PayoutGatewayException(
            string message,
            Exception innerException,
            bool isDefinitiveRejection = false,
            string? errorCode = null)
            : base(message, innerException)
        {
            IsDefinitiveRejection = isDefinitiveRejection;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// True when the provider definitively rejected the request (a 4xx
        /// business error), meaning no transfer was created and the attempt
        /// may be retried under a fresh idempotency key. False for
        /// indeterminate outcomes (timeouts, transport errors, provider 5xx)
        /// where the transfer may exist and the retry must replay the same
        /// idempotency key.
        /// </summary>
        public bool IsDefinitiveRejection { get; }

        /// <summary>
        /// The provider's machine-readable error code, when one was returned.
        /// </summary>
        public string? ErrorCode { get; }
    }
}
