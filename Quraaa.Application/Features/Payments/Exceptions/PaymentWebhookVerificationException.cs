namespace Quraaa.Application.Features.Payments.Exceptions
{
    /// <summary>
    /// Represents a webhook payload that could not be authenticated or parsed safely.
    /// </summary>
    public sealed class PaymentWebhookVerificationException : Exception
    {
        public PaymentWebhookVerificationException(string message)
            : base(message)
        {
        }

        public PaymentWebhookVerificationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
