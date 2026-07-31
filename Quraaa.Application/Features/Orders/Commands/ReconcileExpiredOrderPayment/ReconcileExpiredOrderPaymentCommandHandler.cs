using MediatR;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Orders.Services;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Commands.ReconcileExpiredOrderPayment
{
    public sealed class ReconcileExpiredOrderPaymentCommandHandler
        : IRequestHandler<ReconcileExpiredOrderPaymentCommand, bool>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderPaymentReconciliationService _paymentReconciliationService;

        public ReconcileExpiredOrderPaymentCommandHandler(
            IOrderRepository orderRepository,
            IOrderPaymentReconciliationService paymentReconciliationService)
        {
            _orderRepository = orderRepository;
            _paymentReconciliationService = paymentReconciliationService;
        }

        public async Task<bool> Handle(
            ReconcileExpiredOrderPaymentCommand request,
            CancellationToken cancellationToken)
        {
            if (request.OrderId == Guid.Empty)
            {
                throw new DomainException("Order id is required.");
            }

            var cutoffUtc = NormalizeUtc(request.ExpiredOnOrBeforeUtc);
            var order = await _orderRepository.GetByIdAsync(
                request.OrderId,
                cancellationToken);

            if (order is null
                || order.IsDeleted
                || order.Status != OrderStatus.Pending
                || order.PaymentStatus != PaymentStatus.Pending)
            {
                return false;
            }

            var activeAttempt = FindLatestActiveAttempt(order.PaymentAttempts);

            if (activeAttempt?.ExpiresAtUtc is not DateTime expiresAtUtc
                || NormalizeUtc(expiresAtUtc) > cutoffUtc)
            {
                return false;
            }

            var outcome = await _paymentReconciliationService
                .ReconcileExpiredAttemptAsync(
                    order,
                    activeAttempt,
                    cancellationToken);

            return outcome is
                OrderPaymentReconciliationOutcome.Paid or
                OrderPaymentReconciliationOutcome.Expired;
        }

        private static PaymentAttempt? FindLatestActiveAttempt(
            IEnumerable<PaymentAttempt> attempts)
        {
            return attempts
                .Where(attempt => attempt.Status is
                    PaymentAttemptStatus.Created or
                    PaymentAttemptStatus.CheckoutCreated)
                .OrderByDescending(attempt => attempt.AttemptNumber)
                .FirstOrDefault();
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };
        }
    }
}
