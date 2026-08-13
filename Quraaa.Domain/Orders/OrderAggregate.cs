using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders.Entities;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Orders
{
    public class OrderAggregate : AggregateRoot
    {
        public string OrderNumber { get; private set; } = null!;
        public Guid BuyerUserId { get; private set; }
        public Guid SourceCartId { get; private set; }
        public OrderStatus Status { get; private set; }
        public PaymentStatus PaymentStatus { get; private set; }
        public string Currency { get; private set; } = null!;
        public long SubtotalAmountMinor { get; private set; }
        public long ShippingAmountMinor { get; private set; }
        public long DiscountAmountMinor { get; private set; }
        public long TotalAmountMinor { get; private set; }
        public double? ShippingLatitude { get; private set; }
        public double? ShippingLongitude { get; private set; }
        public DateTime? PaidAtUtc { get; private set; }
        public DateTime? CancelledAtUtc { get; private set; }
        public DateTime? ExpiredAtUtc { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }
        public string? CancellationReason { get; private set; }

        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        private readonly List<PaymentAttempt> _paymentAttempts = new();
        public IReadOnlyCollection<PaymentAttempt> PaymentAttempts => _paymentAttempts.AsReadOnly();

        public bool IsTerminal =>
            Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Expired;

        private OrderAggregate() { }

        private OrderAggregate(
            Guid id,
            string orderNumber,
            Guid buyerUserId,
            Guid sourceCartId,
            string currency,
            double? shippingLatitude,
            double? shippingLongitude)
        {
            Id = id;
            OrderNumber = orderNumber;
            BuyerUserId = buyerUserId;
            SourceCartId = sourceCartId;
            Currency = currency;
            ShippingLatitude = shippingLatitude;
            ShippingLongitude = shippingLongitude;
            Status = OrderStatus.Pending;
            PaymentStatus = PaymentStatus.Pending;
        }

        public static OrderAggregate Create(
            Guid buyerUserId,
            Guid sourceCartId,
            string currency,
            IEnumerable<OrderItemSnapshot> items,
            double? shippingLatitude = null,
            double? shippingLongitude = null,
            long shippingAmountMinor = 0,
            long discountAmountMinor = 0)
        {
            if (buyerUserId == Guid.Empty)
            {
                throw new DomainException("Buyer user id is required.");
            }

            if (sourceCartId == Guid.Empty)
            {
                throw new DomainException("Source cart id is required.");
            }

            var normalizedCurrency = NormalizeCurrency(currency);
            var snapshots = items?.ToList()
                ?? throw new DomainException("Order items are required.");

            if (snapshots.Count == 0)
            {
                throw new DomainException("An order must contain at least one item.");
            }

            if (shippingAmountMinor < 0)
            {
                throw new DomainException("Shipping amount cannot be negative.");
            }

            if (discountAmountMinor < 0)
            {
                throw new DomainException("Discount amount cannot be negative.");
            }

            ValidateShippingLocation(
                shippingLatitude,
                shippingLongitude,
                snapshots.Any(x => x.Format == ListingFormat.Physical));

            var order = new OrderAggregate(
                Guid.NewGuid(),
                GenerateOrderNumber(),
                buyerUserId,
                sourceCartId,
                normalizedCurrency,
                shippingLatitude,
                shippingLongitude);

            foreach (var snapshot in snapshots)
            {
                if (order._items.Any(x => x.ListingId == snapshot.ListingId))
                {
                    throw new DomainException("An order cannot contain duplicate listings.");
                }

                if (snapshot.SellerType == SellerType.User &&
                    snapshot.SellerId == buyerUserId)
                {
                    throw new DomainException("A buyer cannot order their own listing.");
                }

                order._items.Add(OrderItem.Create(order.Id, snapshot));
            }

            order.SubtotalAmountMinor = SumItemTotals(order._items);
            order.ShippingAmountMinor = shippingAmountMinor;
            order.DiscountAmountMinor = discountAmountMinor;

            var amountBeforeDiscount = checked(
                order.SubtotalAmountMinor + order.ShippingAmountMinor);

            if (order.DiscountAmountMinor >= amountBeforeDiscount)
            {
                throw new DomainException("Discount amount must be less than the order amount.");
            }

            order.TotalAmountMinor = checked(
                amountBeforeDiscount - order.DiscountAmountMinor);

            return order;
        }

        public PaymentAttempt StartPaymentAttempt(
            PaymentProvider provider,
            string successUrl,
            string cancelUrl,
            DateTime? expiresAtUtc = null)
        {
            EnsureNotArchived();

            if (Status != OrderStatus.Pending)
            {
                throw new ConflictException("Payment can only be started for a pending order.");
            }

            if (PaymentStatus is PaymentStatus.Paid or PaymentStatus.Cancelled or PaymentStatus.Expired)
            {
                throw new ConflictException("This order cannot accept another payment attempt.");
            }

            if (_paymentAttempts.Any(IsActiveAttempt))
            {
                throw new ConflictException("This order already has an active payment attempt.");
            }

            var attemptNumber = _paymentAttempts.Count == 0
                ? 1
                : checked(_paymentAttempts.Max(x => x.AttemptNumber) + 1);

            var attempt = PaymentAttempt.Create(
                Id,
                attemptNumber,
                provider,
                TotalAmountMinor,
                Currency,
                successUrl,
                cancelUrl,
                NormalizeExpiry(expiresAtUtc ?? DateTime.UtcNow.AddHours(1)));

            _paymentAttempts.Add(attempt);
            PaymentStatus = PaymentStatus.Pending;
            UpdateModificationTime();

            return attempt;
        }

        public void AttachCheckoutSession(
            Guid paymentAttemptId,
            string checkoutSessionId,
            string checkoutUrl,
            DateTime? expiresAtUtc)
        {
            EnsureNotArchived();
            var attempt = GetAttempt(paymentAttemptId);
            EnsureLatestAttempt(attempt);
            attempt.AttachCheckoutSession(checkoutSessionId, checkoutUrl, expiresAtUtc);
            UpdateModificationTime();
        }

        public void MarkPaid(Guid paymentAttemptId, string? paymentIntentId)
        {
            EnsureNotArchived();
            var attempt = GetAttempt(paymentAttemptId);

            if (PaymentStatus == PaymentStatus.Paid)
            {
                var succeededAttempt = _paymentAttempts.SingleOrDefault(
                    candidate => candidate.Status == PaymentAttemptStatus.Succeeded);

                if (succeededAttempt is null || succeededAttempt.Id != attempt.Id)
                {
                    throw new ConflictException(
                        "A different payment attempt already paid this order.");
                }

                attempt.MarkSucceeded(paymentIntentId);
                return;
            }

            if (Status != OrderStatus.Pending ||
                PaymentStatus is PaymentStatus.Cancelled or PaymentStatus.Expired)
            {
                throw new ConflictException("This order cannot transition to paid.");
            }

            EnsureLatestAttempt(attempt);
            attempt.MarkSucceeded(paymentIntentId);

            PaymentStatus = PaymentStatus.Paid;
            PaidAtUtc = DateTime.UtcNow;

            foreach (var digitalItem in _items.Where(x => x.Format == ListingFormat.Digital))
            {
                digitalItem.MarkFulfilled();
            }

            if (_items.All(x => x.FulfillmentStatus == OrderItemFulfillmentStatus.Fulfilled))
            {
                Status = OrderStatus.Completed;
                CompletedAtUtc = DateTime.UtcNow;
            }
            else
            {
                Status = OrderStatus.Processing;
            }

            UpdateModificationTime();
        }

        public void MarkPaymentFailed(
            Guid paymentAttemptId,
            string? failureCode = null,
            string? failureMessage = null)
        {
            EnsureNotArchived();

            if (Status != OrderStatus.Pending || PaymentStatus == PaymentStatus.Paid)
            {
                throw new ConflictException("This order cannot transition to payment failed.");
            }

            var attempt = GetAttempt(paymentAttemptId);
            EnsureLatestAttempt(attempt);
            attempt.MarkFailed(failureCode, failureMessage);

            foreach (var item in _items)
            {
                item.Cancel();
            }

            Status = OrderStatus.Cancelled;
            PaymentStatus = PaymentStatus.Failed;
            var cancellationReason =
                NormalizeOptional(failureMessage) ?? "Payment failed.";
            CancellationReason = cancellationReason.Length <= 500
                ? cancellationReason
                : cancellationReason[..500];
            CancelledAtUtc = DateTime.UtcNow;
            UpdateModificationTime();
        }

        public void Cancel(Guid modifiedBy, string? reason = null)
        {
            EnsureNotArchived();

            if (Status == OrderStatus.Cancelled)
            {
                return;
            }

            if (PaymentStatus == PaymentStatus.Paid || Status != OrderStatus.Pending)
            {
                throw new ConflictException("Only an unpaid pending order can be cancelled.");
            }

            if (reason?.Trim().Length > 500)
            {
                throw new DomainException(
                    "Cancellation reason cannot exceed 500 characters.");
            }

            var activeAttempt = _paymentAttempts.SingleOrDefault(IsActiveAttempt);
            activeAttempt?.MarkCancelled();

            foreach (var item in _items)
            {
                item.Cancel();
            }

            Status = OrderStatus.Cancelled;
            PaymentStatus = PaymentStatus.Cancelled;
            CancellationReason = NormalizeOptional(reason);
            CancelledAtUtc = DateTime.UtcNow;
            UpdateAudit(modifiedBy);
        }

        public void MarkExpired(Guid paymentAttemptId)
        {
            EnsureNotArchived();

            if (Status == OrderStatus.Expired)
            {
                return;
            }

            if (Status != OrderStatus.Pending || PaymentStatus == PaymentStatus.Paid)
            {
                throw new ConflictException("Only an unpaid pending order can expire.");
            }

            var attempt = GetAttempt(paymentAttemptId);
            EnsureLatestAttempt(attempt);
            attempt.MarkExpired();

            foreach (var item in _items)
            {
                item.Cancel();
            }

            Status = OrderStatus.Expired;
            PaymentStatus = PaymentStatus.Expired;
            ExpiredAtUtc = DateTime.UtcNow;
            UpdateModificationTime();
        }

        public void UpdateShippingLocation(
            double latitude,
            double longitude,
            Guid modifiedBy)
        {
            EnsureNotArchived();

            if (!_items.Any(item => item.Format == ListingFormat.Physical))
            {
                throw new ConflictException(
                    "Shipping location can only be changed for orders containing physical items.");
            }

            if (PaymentStatus == PaymentStatus.Paid || Status != OrderStatus.Pending)
            {
                throw new ConflictException("Shipping location can only be changed while an order is unpaid.");
            }

            ValidateShippingLocation(latitude, longitude, requiresLocation: true);

            ShippingLatitude = latitude;
            ShippingLongitude = longitude;
            UpdateAudit(modifiedBy);
        }

        public void MarkItemProcessing(Guid orderItemId, Guid modifiedBy)
        {
            EnsurePaidAndFulfillable();
            GetItem(orderItemId).MarkProcessing();
            Status = OrderStatus.Processing;
            UpdateAudit(modifiedBy);
        }

        public void MarkItemFulfilled(Guid orderItemId, Guid modifiedBy)
        {
            EnsurePaidAndFulfillable();
            var item = GetItem(orderItemId);

            if (item.FulfillmentStatus == OrderItemFulfillmentStatus.Fulfilled)
            {
                return;
            }

            item.MarkFulfilled();

            if (_items.All(x => x.FulfillmentStatus == OrderItemFulfillmentStatus.Fulfilled))
            {
                Status = OrderStatus.Completed;
                CompletedAtUtc ??= DateTime.UtcNow;
            }

            UpdateAudit(modifiedBy);
        }

        public void Archive(Guid archivedBy)
        {
            if (IsDeleted)
            {
                return;
            }

            if (!IsTerminal)
            {
                throw new ConflictException("Only terminal orders can be archived.");
            }

            if (archivedBy == Guid.Empty)
            {
                throw new DomainException("The user archiving the order is required.");
            }

            base.Delete(archivedBy);
        }

        public new void Delete(Guid deletedBy)
        {
            Archive(deletedBy);
        }

        private OrderItem GetItem(Guid orderItemId)
        {
            if (orderItemId == Guid.Empty)
            {
                throw new DomainException("Order item id is required.");
            }

            return _items.FirstOrDefault(x => x.Id == orderItemId)
                ?? throw new NotFoundException("Order item not found.");
        }

        private PaymentAttempt GetAttempt(Guid paymentAttemptId)
        {
            if (paymentAttemptId == Guid.Empty)
            {
                throw new DomainException("Payment attempt id is required.");
            }

            return _paymentAttempts.FirstOrDefault(x => x.Id == paymentAttemptId)
                ?? throw new NotFoundException("Payment attempt not found.");
        }

        private void EnsureLatestAttempt(PaymentAttempt attempt)
        {
            if (_paymentAttempts.Count == 0 ||
                attempt.AttemptNumber != _paymentAttempts.Max(x => x.AttemptNumber))
            {
                throw new ConflictException("Only the latest payment attempt can update this order.");
            }
        }

        private void EnsurePaidAndFulfillable()
        {
            EnsureNotArchived();

            if (PaymentStatus != PaymentStatus.Paid ||
                Status is not (OrderStatus.Processing or OrderStatus.Completed))
            {
                throw new ConflictException("Only paid orders can be fulfilled.");
            }
        }

        private void EnsureNotArchived()
        {
            if (IsDeleted)
            {
                throw new ConflictException("An archived order cannot be changed.");
            }
        }

        private static bool IsActiveAttempt(PaymentAttempt attempt)
        {
            return attempt.Status is
                PaymentAttemptStatus.Created or
                PaymentAttemptStatus.CheckoutCreated;
        }

        private static long SumItemTotals(IEnumerable<OrderItem> items)
        {
            var total = 0L;

            foreach (var item in items)
            {
                total = checked(total + item.TotalPriceMinor);
            }

            return total;
        }

        private static string NormalizeCurrency(string currency)
        {
            if (string.IsNullOrWhiteSpace(currency))
            {
                throw new DomainException("Currency is required.");
            }

            var normalized = currency.Trim().ToLowerInvariant();

            if (normalized.Length != 3 || normalized.Any(x => x is < 'a' or > 'z'))
            {
                throw new DomainException("Currency must be a three-letter ISO code.");
            }

            return normalized;
        }

        private static void ValidateShippingLocation(
            double? latitude,
            double? longitude,
            bool requiresLocation)
        {
            if (latitude.HasValue != longitude.HasValue)
            {
                throw new DomainException("Shipping latitude and longitude must be provided together.");
            }

            if (requiresLocation && (!latitude.HasValue || !longitude.HasValue))
            {
                throw new DomainException("Shipping location is required for physical order items.");
            }

            if (!requiresLocation && (latitude.HasValue || longitude.HasValue))
            {
                throw new DomainException(
                    "Shipping location is only allowed for orders containing physical items.");
            }

            if (latitude.HasValue && !double.IsFinite(latitude.Value))
            {
                throw new DomainException("Shipping latitude must be a finite number.");
            }

            if (longitude.HasValue && !double.IsFinite(longitude.Value))
            {
                throw new DomainException("Shipping longitude must be a finite number.");
            }

            if (latitude is < -90 or > 90)
            {
                throw new DomainException("Shipping latitude must be between -90 and 90.");
            }

            if (longitude is < -180 or > 180)
            {
                throw new DomainException("Shipping longitude must be between -180 and 180.");
            }
        }

        private static string GenerateOrderNumber()
        {
            var datePart = DateTime.UtcNow.ToString(
                "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture);
            var randomPart = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            return $"ORD-{datePart}-{randomPart}";
        }

        private static DateTime NormalizeExpiry(DateTime expiresAt)
        {
            return expiresAt.Kind switch
            {
                DateTimeKind.Utc => expiresAt,
                DateTimeKind.Local => expiresAt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
