using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Shared.Results;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Commands.CancelOrder
{
    public class CancelOrderCommandHandler
        : BaseApplicationService<CancelOrderCommandHandler>,
          IRequestHandler<CancelOrderCommand, AppResult<OrderResponse>>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IPaymentGateway _paymentGateway;

        public CancelOrderCommandHandler(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IListingRepository listingRepository,
            IPaymentGateway paymentGateway,
            ILogger<CancelOrderCommandHandler> logger,
            IServiceProvider serviceProvider)
            : base(logger, serviceProvider)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _listingRepository = listingRepository;
            _paymentGateway = paymentGateway;
        }

        public async Task<AppResult<OrderResponse>> Handle(
            CancelOrderCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteAsync(request, async () =>
            {
                var order = await _orderRepository.GetByIdForBuyerAsync(
                    request.OrderId,
                    request.BuyerUserId,
                    cancellationToken)
                    ?? throw new NotFoundException("Order not found.");

                if (order.Status != OrderStatus.Pending
                    || order.PaymentStatus == PaymentStatus.Paid)
                {
                    throw new ConflictException("Only an unpaid pending order can be cancelled.");
                }

                var activeAttempt = FindActiveAttempt(order.PaymentAttempts);

                if (activeAttempt?.Status == PaymentAttemptStatus.Created)
                {
                    throw new ConflictException(
                        "Checkout session creation is still in progress. Retry cancellation shortly.");
                }

                if (activeAttempt?.Status == PaymentAttemptStatus.CheckoutCreated
                    && !string.IsNullOrWhiteSpace(activeAttempt.CheckoutSessionId))
                {
                    await _paymentGateway.ExpireCheckoutSessionAsync(
                        activeAttempt.CheckoutSessionId,
                        $"expire:{activeAttempt.Id:N}",
                        cancellationToken);
                }

                var cart = await _cartRepository.GetByIdAsync(
                    order.SourceCartId,
                    cancellationToken);

                order.Cancel(request.BuyerUserId, request.Reason);

                foreach (var item in order.Items.Where(
                    item => item.Format == ListingFormat.Physical))
                {
                    var listing = await _listingRepository.GetByIdForInventoryAsync(
                        item.ListingId,
                        cancellationToken)
                        ?? throw new NotFoundException("Reserved listing not found.");

                    listing.ReleaseReservedStock(item.Quantity, request.BuyerUserId);
                }

                if (cart is null)
                {
                    Logger.LogError(
                        "Cancelled order {OrderId} has no mutable source cart {CartId}; cancellation will continue.",
                        order.Id,
                        order.SourceCartId);
                }
                else
                {
                    try
                    {
                        cart.ReopenAfterPaymentFailure(order.Id);
                    }
                    catch (ConflictException exception)
                    {
                        Logger.LogError(
                            exception,
                            "Cancelled order {OrderId} could not reopen source cart {CartId}; cancellation will continue.",
                            order.Id,
                            order.SourceCartId);
                    }
                }

                await _orderRepository.SaveChangesAsync(cancellationToken);

                return order.ToResponse();
            }, "Order cancelled successfully");
        }

        private static PaymentAttempt? FindActiveAttempt(
            IEnumerable<PaymentAttempt> attempts)
        {
            return attempts
                .Where(attempt => attempt.Status is
                    PaymentAttemptStatus.Created or
                    PaymentAttemptStatus.CheckoutCreated)
                .OrderByDescending(attempt => attempt.AttemptNumber)
                .FirstOrDefault();
        }
    }
}
