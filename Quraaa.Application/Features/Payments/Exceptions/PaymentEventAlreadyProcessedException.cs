namespace Quraaa.Application.Features.Payments.Exceptions
{
    public sealed class PaymentEventAlreadyProcessedException : Exception
    {
        public PaymentEventAlreadyProcessedException()
            : base("The payment event was already processed.")
        {
        }
    }
}
