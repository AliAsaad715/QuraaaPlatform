using Quraaa.Domain.Marketplace.Enums;
using Quraaa.Domain.Orders.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Orders.Entities
{
    public class OrderItem : Entity
    {
        public Guid OrderId { get; private set; }
        public Guid BookId { get; private set; }
        public Guid ListingId { get; private set; }
        public SellerType SellerType { get; private set; }
        public Guid SellerId { get; private set; }
        public ListingFormat Format { get; private set; }
        public string BookTitleSnapshot { get; private set; } = null!;
        public string BookAuthorSnapshot { get; private set; } = null!;
        public string? BookCoverImageUrlSnapshot { get; private set; }
        public string SellerNameSnapshot { get; private set; } = null!;
        public string? DigitalAssetUrlSnapshot { get; private set; }
        public BookCondition? ConditionSnapshot { get; private set; }
        public int Quantity { get; private set; }
        public long UnitPriceMinor { get; private set; }
        public long TotalPriceMinor { get; private set; }
        public OrderItemFulfillmentStatus FulfillmentStatus { get; private set; }
        public DateTime? FulfilledAtUtc { get; private set; }
        public DateTime? CancelledAtUtc { get; private set; }

        private OrderItem() { }

        private OrderItem(
            Guid id,
            Guid orderId,
            OrderItemSnapshot snapshot)
        {
            Id = id;
            OrderId = orderId;
            BookId = snapshot.BookId;
            ListingId = snapshot.ListingId;
            SellerType = snapshot.SellerType;
            SellerId = snapshot.SellerId;
            Format = snapshot.Format;
            BookTitleSnapshot = snapshot.BookTitle.Trim();
            BookAuthorSnapshot = snapshot.BookAuthor.Trim();
            BookCoverImageUrlSnapshot = NormalizeOptional(snapshot.BookCoverImageUrl);
            SellerNameSnapshot = snapshot.SellerName.Trim();
            DigitalAssetUrlSnapshot = NormalizeOptional(snapshot.DigitalAssetUrl);
            ConditionSnapshot = snapshot.Condition;
            Quantity = snapshot.Quantity;
            UnitPriceMinor = snapshot.UnitPriceMinor;
            TotalPriceMinor = checked(snapshot.UnitPriceMinor * snapshot.Quantity);
            FulfillmentStatus = OrderItemFulfillmentStatus.Pending;
        }

        internal static OrderItem Create(Guid orderId, OrderItemSnapshot snapshot)
        {
            if (orderId == Guid.Empty)
            {
                throw new DomainException("Order id is required.");
            }

            if (snapshot.BookId == Guid.Empty)
            {
                throw new DomainException("Book id is required for an order item.");
            }

            if (snapshot.ListingId == Guid.Empty)
            {
                throw new DomainException("Listing id is required for an order item.");
            }

            if (snapshot.SellerId == Guid.Empty)
            {
                throw new DomainException("Seller id is required for an order item.");
            }

            if (!Enum.IsDefined(snapshot.SellerType))
            {
                throw new DomainException("Seller type is invalid.");
            }

            if (!Enum.IsDefined(snapshot.Format))
            {
                throw new DomainException("Listing format is invalid.");
            }

            if (string.IsNullOrWhiteSpace(snapshot.BookTitle))
            {
                throw new DomainException("Book title is required for an order item.");
            }

            if (string.IsNullOrWhiteSpace(snapshot.BookAuthor))
            {
                throw new DomainException("Book author is required for an order item.");
            }

            if (string.IsNullOrWhiteSpace(snapshot.SellerName))
            {
                throw new DomainException("Seller name is required for an order item.");
            }

            if (snapshot.Format == ListingFormat.Physical && snapshot.Condition is null)
            {
                throw new DomainException("Book condition is required for a physical order item.");
            }

            if (snapshot.Format == ListingFormat.Physical && snapshot.DigitalAssetUrl is not null)
            {
                throw new DomainException("A digital asset URL is not applicable to a physical order item.");
            }

            if (snapshot.Format == ListingFormat.Digital && snapshot.Condition is not null)
            {
                throw new DomainException("Book condition is not applicable to a digital order item.");
            }

            if (snapshot.Format == ListingFormat.Digital &&
                string.IsNullOrWhiteSpace(snapshot.DigitalAssetUrl))
            {
                throw new DomainException("A digital asset URL is required for a digital order item.");
            }

            if (snapshot.Quantity <= 0)
            {
                throw new DomainException("Order item quantity must be greater than zero.");
            }

            if (snapshot.UnitPriceMinor <= 0)
            {
                throw new DomainException("Order item unit price must be greater than zero.");
            }

            _ = checked(snapshot.UnitPriceMinor * snapshot.Quantity);

            return new OrderItem(Guid.NewGuid(), orderId, snapshot);
        }

        internal void MarkProcessing()
        {
            if (FulfillmentStatus == OrderItemFulfillmentStatus.Processing)
            {
                return;
            }

            if (FulfillmentStatus != OrderItemFulfillmentStatus.Pending)
            {
                throw new ConflictException("Only pending order items can start fulfillment.");
            }

            FulfillmentStatus = OrderItemFulfillmentStatus.Processing;
        }

        internal void MarkFulfilled()
        {
            if (FulfillmentStatus == OrderItemFulfillmentStatus.Fulfilled)
            {
                return;
            }

            if (FulfillmentStatus is not (
                OrderItemFulfillmentStatus.Pending or
                OrderItemFulfillmentStatus.Processing))
            {
                throw new ConflictException("This order item cannot be fulfilled.");
            }

            FulfillmentStatus = OrderItemFulfillmentStatus.Fulfilled;
            FulfilledAtUtc = DateTime.UtcNow;
        }

        internal void Cancel()
        {
            if (FulfillmentStatus == OrderItemFulfillmentStatus.Cancelled)
            {
                return;
            }

            if (FulfillmentStatus == OrderItemFulfillmentStatus.Fulfilled)
            {
                throw new ConflictException("A fulfilled order item cannot be cancelled.");
            }

            FulfillmentStatus = OrderItemFulfillmentStatus.Cancelled;
            CancelledAtUtc = DateTime.UtcNow;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
