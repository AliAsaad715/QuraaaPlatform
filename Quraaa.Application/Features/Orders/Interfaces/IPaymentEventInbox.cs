namespace Quraaa.Application.Features.Orders.Interfaces
{
    public interface IPaymentEventInbox
    {
        Task<bool> ExistsAsync(
            string provider,
            string eventId,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            string provider,
            string eventId,
            string eventType,
            Guid? orderId = null,
            Guid? paymentAttemptId = null,
            CancellationToken cancellationToken = default);
    }
}
