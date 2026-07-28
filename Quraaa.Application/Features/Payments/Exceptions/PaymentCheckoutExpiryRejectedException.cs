namespace Quraaa.Application.Features.Payments.Exceptions
{
    /// <summary>
    /// Indicates that the payment provider rejected checkout creation because
    /// the persisted expiry is no longer far enough in the future.
    /// </summary>
    public sealed class PaymentCheckoutExpiryRejectedException : Exception
    {
        public PaymentCheckoutExpiryRejectedException(Exception innerException)
            : base(
                "The persisted checkout expiry is no longer accepted by the payment provider.",
                innerException)
        {
        }
    }
}
