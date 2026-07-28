using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Purchases
{
    public class BookPurchaseAggregate : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public Guid BookId { get; private set; }
        public Guid ListingId { get; private set; }
        public Guid? OrderId { get; private set; }
        public Guid? OrderItemId { get; private set; }
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal TotalPrice => Quantity * UnitPrice;

        private BookPurchaseAggregate() { }

        private BookPurchaseAggregate(
            Guid id,
            Guid userId,
            Guid bookId,
            Guid listingId,
            Guid? orderId,
            Guid? orderItemId,
            int quantity,
            decimal unitPrice)
        {
            Id = id;
            UserId = userId;
            BookId = bookId;
            ListingId = listingId;
            OrderId = orderId;
            OrderItemId = orderItemId;
            Quantity = quantity;
            UnitPrice = unitPrice;
        }

        public static BookPurchaseAggregate Create(
            Guid userId,
            Guid bookId,
            Guid listingId,
            int quantity,
            decimal unitPrice)
        {
            return Create(
                userId,
                bookId,
                listingId,
                quantity,
                unitPrice,
                null,
                null);
        }

        public static BookPurchaseAggregate Create(
            Guid userId,
            Guid bookId,
            Guid listingId,
            int quantity,
            decimal unitPrice,
            Guid? orderId,
            Guid? orderItemId)
        {
            if (userId == Guid.Empty)
            {
                throw new DomainException("User id is required for a purchase.");
            }

            if (bookId == Guid.Empty)
            {
                throw new DomainException("Book id is required for a purchase.");
            }

            if (listingId == Guid.Empty)
            {
                throw new DomainException("Listing id is required for a purchase.");
            }

            if (quantity <= 0)
            {
                throw new DomainException("Purchase quantity must be greater than zero.");
            }

            if (unitPrice < 0)
            {
                throw new DomainException("Unit price cannot be negative.");
            }

            if (orderId.HasValue != orderItemId.HasValue)
            {
                throw new DomainException("Order id and order item id must be provided together.");
            }

            if (orderId == Guid.Empty)
            {
                throw new DomainException("Order id cannot be empty.");
            }

            if (orderItemId == Guid.Empty)
            {
                throw new DomainException("Order item id cannot be empty.");
            }

            return new BookPurchaseAggregate(
                Guid.NewGuid(),
                userId,
                bookId,
                listingId,
                orderId,
                orderItemId,
                quantity,
                unitPrice);
        }
    }
}
