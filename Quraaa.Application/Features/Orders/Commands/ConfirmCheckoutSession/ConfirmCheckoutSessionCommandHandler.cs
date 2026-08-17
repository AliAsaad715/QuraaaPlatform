using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Orders.Services;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Commands.ConfirmCheckoutSession
{
    public class ConfirmCheckoutSessionCommandHandler
        : BaseApplicationService<ConfirmCheckoutSessionCommandHandler>,
          IRequestHandler<ConfirmCheckoutSessionCommand, AppResult<CheckoutStatusResponse>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IOrderPaymentFinalizationService _paymentFinalizationService;
        private readonly ISellerPayoutDispatchSignal _payoutDispatchSignal;

        public ConfirmCheckoutSessionCommandHandler(
            IOrderRepository orderRepository,
            IPaymentGateway paymentGateway,
            IOrderPaymentFinalizationService paymentFinalizationService,
            ISellerPayoutDispatchSignal payoutDispatchSignal,
            ILogger<ConfirmCheckoutSessionCommandHandler> logger,
            IServiceProvider serviceProvider) : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
            _paymentGateway = paymentGateway;
            _paymentFinalizationService = paymentFinalizationService;
            _payoutDispatchSignal = payoutDispatchSignal;
        }

        public async Task<AppResult<CheckoutStatusResponse>> Handle(
            ConfirmCheckoutSessionCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync<ConfirmCheckoutSessionCommand, CheckoutStatusResponse>(request, async () =>
            {
                var sessionId = request.SessionId.Trim();

                var order = await _orderRepository.GetByCheckoutSessionIdAsync(
                    sessionId,
                    cancellationToken);

                // Scoped to the buyer: a session id must never reveal, let alone
                // settle, somebody else's order.
                if (order is null || order.BuyerUserId != request.BuyerUserId)
                {
                    throw new NotFoundException("No checkout was found for this session.");
                }

                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    return Describe(order, paid: true);
                }

                var attempt = order.PaymentAttempts
                    .FirstOrDefault(candidate => string.Equals(
                        candidate.CheckoutSessionId,
                        sessionId,
                        StringComparison.Ordinal));

                if (attempt is null)
                {
                    throw new NotFoundException("No checkout was found for this session.");
                }

                var session = await _paymentGateway.GetCheckoutSessionAsync(
                    sessionId,
                    cancellationToken);

                if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                {
                    // Genuinely not paid (yet). Never expire the session here —
                    // the buyer may still be completing it in another tab, and
                    // the scheduled reconciliation owns that decision.
                    return Describe(order, paid: false);
                }

                // Same finalizer the webhook uses, with the same amount, currency
                // and live-mode guards, so this cannot mark an order paid on
                // weaker evidence than a webhook would.
                await _paymentFinalizationService.CompletePaidOrderAsync(
                    order,
                    attempt,
                    session,
                    cancellationToken);

                await _orderRepository.SaveChangesAsync(cancellationToken);

                // Seller profit shares were staged in that same transaction.
                _payoutDispatchSignal.RequestImmediateProcessing();

                Logger.LogInformation(
                    "Order {OrderId} was confirmed paid from the buyer's return, ahead of the provider webhook.",
                    order.Id);

                return Describe(order, paid: true);
            }, "Checkout confirmed successfully");
        }

        private static CheckoutStatusResponse Describe(OrderAggregate order, bool paid)
        {
            // "Pending" tells the client the answer may still change, so it polls
            // instead of telling the buyer their payment failed.
            var pending = !paid
                && order.PaymentStatus == PaymentStatus.Pending
                && order.Status == OrderStatus.Pending;

            return new CheckoutStatusResponse(
                paid,
                pending,
                order.Id,
                order.OrderNumber,
                order.Status.ToString(),
                order.PaymentStatus.ToString());
        }
    }
}
