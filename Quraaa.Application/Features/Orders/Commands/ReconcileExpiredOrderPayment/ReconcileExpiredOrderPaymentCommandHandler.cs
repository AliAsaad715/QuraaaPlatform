using MediatR;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Application.Features.Orders.Commands.ReconcileExpiredOrderPayment
{
    public sealed class ReconcileExpiredOrderPaymentCommandHandler
        : IRequestHandler<ReconcileExpiredOrderPaymentCommand, bool>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IPaymentGateway _paymentGateway;

        public ReconcileExpiredOrderPaymentCommandHandler(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IListingRepository listingRepository,
            IPaymentGateway paymentGateway)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _listingRepository = listingRepository;
            _paymentGateway = paymentGateway;
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

            if (activeAttempt.Status == PaymentAttemptStatus.CheckoutCreated
                && !string.IsNullOrWhiteSpace(activeAttempt.CheckoutSessionId))
            {
                // Do not compensate a Session Stripe already considers
                // complete; its paid webhook remains authoritative.
                await _paymentGateway.ExpireCheckoutSessionAsync(
                    activeAttempt.CheckoutSessionId,
                    $"reconcile-expired:{activeAttempt.Id:N}",
                    cancellationToken);
            }

            // Resolve every stock row before changing state. SaveChanges commits
            // the order, cart, and listing updates as one transaction.
            var physicalListings = new List<(ListingAggregate Listing, int Quantity)>();

            foreach (var item in order.Items.Where(
                item => item.Format == ListingFormat.Physical))
            {
                var listing = await _listingRepository.GetByIdForInventoryAsync(
                    item.ListingId,
                    cancellationToken)
                    ?? throw new NotFoundException(
                        $"Reserved listing {item.ListingId} was not found.");

                physicalListings.Add((listing, item.Quantity));
            }

            var cart = await _cartRepository.GetByIdAsync(
                order.SourceCartId,
                cancellationToken);

            order.MarkExpired(activeAttempt.Id);

            foreach (var (listing, quantity) in physicalListings)
            {
                listing.ReleaseReservedStock(quantity, order.BuyerUserId);
            }

            if (cart is not null
                && cart.Status == CartStatus.PendingPayment
                && cart.PendingOrderId == order.Id)
            {
                cart.ReopenAfterPaymentFailure(order.Id);
            }

            // Order, cart, and listing concurrency tokens make competing
            // webhook/reconciler attempts roll back instead of double-releasing.
            await _orderRepository.SaveChangesAsync(cancellationToken);
            return true;
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
