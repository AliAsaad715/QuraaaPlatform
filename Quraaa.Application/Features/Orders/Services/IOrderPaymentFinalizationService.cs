using Quraaa.Application.Features.Payments.Common;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Entities;

namespace Quraaa.Application.Features.Orders.Services
{
    public interface IOrderPaymentFinalizationService
    {
        Task CompletePaidOrderAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            PaymentWebhookEventData paymentEvent,
            CancellationToken cancellationToken = default);

        Task CompletePaidOrderAsync(
            OrderAggregate order,
            PaymentAttempt attempt,
            PaymentCheckoutSessionState checkoutSession,
            CancellationToken cancellationToken = default);
    }
}
