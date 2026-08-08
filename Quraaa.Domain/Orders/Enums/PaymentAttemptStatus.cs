namespace Quraaa.Domain.Orders.Enums
{
    public enum PaymentAttemptStatus
    {
        Created = 1,
        CheckoutCreated = 2,
        Succeeded = 3,
        Failed = 4,
        Cancelled = 5,
        Expired = 6
    }
}
