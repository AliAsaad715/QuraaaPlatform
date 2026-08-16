using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Application.Features.Orders.Common;

namespace Quraaa.Application.Features.Orders.Interfaces
{
    public interface IOrderRepository
    {
        Task AddAsync(
            OrderAggregate order,
            CancellationToken cancellationToken = default);

        Task<OrderAggregate?> GetByIdAsync(
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<OrderAggregate?> GetByIdForBuyerAsync(
            Guid orderId,
            Guid buyerUserId,
            CancellationToken cancellationToken = default);

        Task<OrderAggregate?> GetByIdForSellerAsync(
            Guid orderId,
            Guid orderItemId,
            Guid sellerUserId,
            Guid? sellerLibraryId,
            CancellationToken cancellationToken = default);

        Task<OrderAggregate?> GetActiveBySourceCartIdAsync(
            Guid sourceCartId,
            CancellationToken cancellationToken = default);

        Task<OrderAggregate?> GetByCheckoutSessionIdAsync(
            string stripeCheckoutSessionId,
            CancellationToken cancellationToken = default);

        Task<bool> IsCheckoutSessionAttachedAsync(
            Guid orderId,
            Guid paymentAttemptId,
            string stripeCheckoutSessionId,
            CancellationToken cancellationToken = default);

        Task<bool> IsPaymentAttemptAwaitingCheckoutAttachmentAsync(
            Guid orderId,
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default);

        Task<bool> HasActiveStockReservationAsync(
            Guid listingId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ExpiredOrderPaymentCandidate>>
            GetExpiredPendingOrderCandidatesAsync(
            DateTime expiredOnOrBeforeUtc,
            ExpiredOrderPaymentCandidate? after,
            int take,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<SellerOrderItemResponse> Items, int TotalCount)>
            GetSellerOrderItemsAsync(
                Guid sellerUserId,
                Guid? sellerLibraryId,
                int pageNumber,
                int pageSize,
                OrderItemFulfillmentStatus? fulfillmentStatus = null,
                CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<OrderAggregate> Items, int TotalCount)> GetBuyerOrdersAsync(
            Guid buyerUserId,
            int pageNumber,
            int pageSize,
            OrderStatus? status = null,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
