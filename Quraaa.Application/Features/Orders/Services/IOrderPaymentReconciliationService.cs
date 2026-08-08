using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;

namespace Quraaa.Application.Features.Orders.Services
{
    public enum OrderPaymentReconciliationOutcome
    {
        Paid,
        Expired,
        AwaitingPayment
    }

    public interface IOrderPaymentReconciliationService
    {
        Task<OrderPaymentReconciliationOutcome> ReconcileExpiredAttemptAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            CancellationToken cancellationToken = default);
    }
}
