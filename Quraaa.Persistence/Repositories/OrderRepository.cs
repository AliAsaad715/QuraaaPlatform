using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Application.Features.Payments.Exceptions;
using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public sealed class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(
            OrderAggregate order,
            CancellationToken cancellationToken = default)
        {
            await _context.Orders.AddAsync(order, cancellationToken);
        }

        public Task<OrderAggregate?> GetByIdAsync(
            Guid orderId,
            CancellationToken cancellationToken = default)
        {
            return AggregateQuery()
                .FirstOrDefaultAsync(
                    order => order.Id == orderId,
                    cancellationToken);
        }

        public Task<OrderAggregate?> GetByIdForBuyerAsync(
            Guid orderId,
            Guid buyerUserId,
            CancellationToken cancellationToken = default)
        {
            return AggregateQuery()
                .FirstOrDefaultAsync(
                    order => order.Id == orderId
                        && order.BuyerUserId == buyerUserId
                        && !order.IsDeleted,
                    cancellationToken);
        }

        public Task<OrderAggregate?> GetByIdForBuyerIncludingArchivedAsync(
            Guid orderId,
            Guid buyerUserId,
            CancellationToken cancellationToken = default)
        {
            return AggregateQuery()
                .FirstOrDefaultAsync(
                    order => order.Id == orderId
                        && order.BuyerUserId == buyerUserId,
                    cancellationToken);
        }

        public Task<OrderAggregate?> GetByIdForSellerAsync(
            Guid orderId,
            Guid orderItemId,
            Guid sellerUserId,
            Guid? sellerLibraryId,
            CancellationToken cancellationToken = default)
        {
            return AggregateQuery()
                .FirstOrDefaultAsync(
                    order => order.Id == orderId
                        && !order.IsDeleted
                        && order.Items.Any(item =>
                            item.Id == orderItemId
                            && ((item.SellerType == SellerType.User
                                    && item.SellerId == sellerUserId)
                                || (sellerLibraryId.HasValue
                                    && item.SellerType == SellerType.Library
                                    && item.SellerId == sellerLibraryId.Value))),
                    cancellationToken);
        }

        public Task<OrderAggregate?> GetActiveBySourceCartIdAsync(
            Guid sourceCartId,
            CancellationToken cancellationToken = default)
        {
            return AggregateQuery()
                .FirstOrDefaultAsync(
                    order => order.SourceCartId == sourceCartId
                        && (order.Status == OrderStatus.Pending
                            || order.Status == OrderStatus.Processing)
                        && !order.IsDeleted,
                    cancellationToken);
        }

        public Task<OrderAggregate?> GetByCheckoutSessionIdAsync(
            string stripeCheckoutSessionId,
            CancellationToken cancellationToken = default)
        {
            return AggregateQuery()
                .FirstOrDefaultAsync(
                    order => order.PaymentAttempts.Any(attempt =>
                            attempt.CheckoutSessionId == stripeCheckoutSessionId),
                    cancellationToken);
        }

        public Task<bool> IsCheckoutSessionAttachedAsync(
            Guid orderId,
            Guid paymentAttemptId,
            string stripeCheckoutSessionId,
            CancellationToken cancellationToken = default)
        {
            return _context.Orders
                .AsNoTracking()
                .Where(order => order.Id == orderId)
                .SelectMany(order => order.PaymentAttempts)
                .AnyAsync(
                    attempt => attempt.Id == paymentAttemptId
                        && attempt.CheckoutSessionId == stripeCheckoutSessionId,
                    cancellationToken);
        }

        public Task<bool> IsPaymentAttemptAwaitingCheckoutAttachmentAsync(
            Guid orderId,
            Guid paymentAttemptId,
            CancellationToken cancellationToken = default)
        {
            return _context.Orders
                .AsNoTracking()
                .AnyAsync(
                    order => order.Id == orderId
                        && order.Status == OrderStatus.Pending
                        && order.PaymentStatus == PaymentStatus.Pending
                        && order.PaymentAttempts.Any(attempt =>
                            attempt.Id == paymentAttemptId
                            && attempt.Status == PaymentAttemptStatus.Created),
                    cancellationToken);
        }

        public Task<bool> HasActiveStockReservationAsync(
            Guid listingId,
            CancellationToken cancellationToken = default)
        {
            return _context.Orders
                .AsNoTracking()
                .AnyAsync(
                    order => !order.IsDeleted
                        && order.Status == OrderStatus.Pending
                        && order.PaymentStatus == PaymentStatus.Pending
                        && order.Items.Any(item =>
                            item.ListingId == listingId
                            && item.Format == ListingFormat.Physical),
                    cancellationToken);
        }

        public async Task<IReadOnlyCollection<Guid>> GetExpiredPendingOrderIdsAsync(
            DateTime expiredOnOrBeforeUtc,
            int take,
            CancellationToken cancellationToken = default)
        {
            if (take is < 1 or > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(take),
                    "The reconciliation batch size must be between 1 and 100.");
            }

            var normalizedCutoffUtc = expiredOnOrBeforeUtc.Kind switch
            {
                DateTimeKind.Utc => expiredOnOrBeforeUtc,
                DateTimeKind.Local => expiredOnOrBeforeUtc.ToUniversalTime(),
                _ => DateTime.SpecifyKind(expiredOnOrBeforeUtc, DateTimeKind.Utc)
            };

            return await _context.Orders
                .AsNoTracking()
                .Where(order =>
                    !order.IsDeleted
                    && order.Status == OrderStatus.Pending
                    && order.PaymentStatus == PaymentStatus.Pending
                    && order.PaymentAttempts.Any(attempt =>
                        (attempt.Status == PaymentAttemptStatus.Created
                            || attempt.Status == PaymentAttemptStatus.CheckoutCreated)
                        && attempt.ExpiresAtUtc.HasValue
                        && attempt.ExpiresAtUtc.Value <= normalizedCutoffUtc))
                .OrderBy(order => order.PaymentAttempts
                    .Where(attempt =>
                        (attempt.Status == PaymentAttemptStatus.Created
                            || attempt.Status == PaymentAttemptStatus.CheckoutCreated)
                        && attempt.ExpiresAtUtc.HasValue)
                    .Min(attempt => attempt.ExpiresAtUtc))
                .ThenBy(order => order.Id)
                .Select(order => order.Id)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<(
            IReadOnlyCollection<SellerOrderItemResponse> Items,
            int TotalCount)> GetSellerOrderItemsAsync(
            Guid sellerUserId,
            Guid? sellerLibraryId,
            int pageNumber,
            int pageSize,
            OrderItemFulfillmentStatus? fulfillmentStatus = null,
            CancellationToken cancellationToken = default)
        {
            var query =
                from order in _context.Orders.AsNoTracking()
                from item in order.Items
                where order.PaymentStatus == PaymentStatus.Paid
                    && item.Format == ListingFormat.Physical
                    && ((item.SellerType == SellerType.User
                            && item.SellerId == sellerUserId)
                        || (sellerLibraryId.HasValue
                            && item.SellerType == SellerType.Library
                            && item.SellerId == sellerLibraryId.Value))
                select new SellerOrderItemProjection
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    OrderStatus = order.Status,
                    PaymentStatus = order.PaymentStatus,
                    Currency = order.Currency,
                    OrderItemId = item.Id,
                    ListingId = item.ListingId,
                    BookId = item.BookId,
                    Title = item.BookTitleSnapshot,
                    Author = item.BookAuthorSnapshot,
                    CoverImageUrl = item.BookCoverImageUrlSnapshot,
                    Condition = item.ConditionSnapshot,
                    Quantity = item.Quantity,
                    UnitPriceMinor = item.UnitPriceMinor,
                    LineTotalMinor = item.TotalPriceMinor,
                    FulfillmentStatus = item.FulfillmentStatus,
                    ShippingLatitude = order.ShippingLatitude,
                    ShippingLongitude = order.ShippingLongitude,
                    CreationTime = order.CreationTime,
                    PaidAtUtc = order.PaidAtUtc
                };

            if (fulfillmentStatus.HasValue)
            {
                query = query.Where(
                    row => row.FulfillmentStatus == fulfillmentStatus.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var rows = await query
                .OrderByDescending(row => row.PaidAtUtc)
                .ThenByDescending(row => row.CreationTime)
                .ThenBy(row => row.OrderItemId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = rows.Select(row => new SellerOrderItemResponse(
                    row.OrderId,
                    row.OrderNumber,
                    row.OrderStatus,
                    row.PaymentStatus,
                    row.Currency,
                    row.OrderItemId,
                    row.ListingId,
                    row.BookId,
                    row.Title,
                    row.Author,
                    row.CoverImageUrl,
                    row.Condition,
                    row.Quantity,
                    row.UnitPriceMinor / 100m,
                    row.LineTotalMinor / 100m,
                    row.FulfillmentStatus,
                    row.ShippingLatitude.HasValue
                        && row.ShippingLongitude.HasValue
                        ? new ShippingLocationResponse(
                            row.ShippingLatitude.Value,
                            row.ShippingLongitude.Value)
                        : null,
                    row.CreationTime,
                    row.PaidAtUtc))
                .ToList();

            return (items, totalCount);
        }

        public async Task<(IReadOnlyCollection<OrderAggregate> Items, int TotalCount)> GetBuyerOrdersAsync(
            Guid buyerUserId,
            int pageNumber,
            int pageSize,
            OrderStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            var baseQuery = _context.Orders
                .AsNoTracking()
                .Where(order => order.BuyerUserId == buyerUserId && !order.IsDeleted);

            if (status.HasValue)
            {
                baseQuery = baseQuery.Where(order => order.Status == status.Value);
            }

            var totalCount = await baseQuery.CountAsync(cancellationToken);

            var items = await baseQuery
                .OrderByDescending(order => order.CreationTime)
                .ThenByDescending(order => order.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(order => order.Items)
                .Include(order => order.PaymentAttempts)
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Checkout state changed concurrently. Retry the operation.");
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "IX_Orders_SourceCartId"
                })
            {
                throw new ConflictException(
                    "Another order is already being created from this cart. Reload it and retry.");
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "IX_ProcessedPaymentEvents_Provider_EventId"
                })
            {
                throw new PaymentEventAlreadyProcessedException();
            }
        }

        private IQueryable<OrderAggregate> AggregateQuery()
        {
            return _context.Orders
                .Include(order => order.Items)
                .Include(order => order.PaymentAttempts)
                .AsSplitQuery();
        }

        private sealed class SellerOrderItemProjection
        {
            public Guid OrderId { get; init; }
            public required string OrderNumber { get; init; }
            public OrderStatus OrderStatus { get; init; }
            public PaymentStatus PaymentStatus { get; init; }
            public required string Currency { get; init; }
            public Guid OrderItemId { get; init; }
            public Guid ListingId { get; init; }
            public Guid BookId { get; init; }
            public required string Title { get; init; }
            public required string Author { get; init; }
            public string? CoverImageUrl { get; init; }
            public BookCondition? Condition { get; init; }
            public int Quantity { get; init; }
            public long UnitPriceMinor { get; init; }
            public long LineTotalMinor { get; init; }
            public OrderItemFulfillmentStatus FulfillmentStatus { get; init; }
            public double? ShippingLatitude { get; init; }
            public double? ShippingLongitude { get; init; }
            public DateTime CreationTime { get; init; }
            public DateTime? PaidAtUtc { get; init; }
        }
    }
}
