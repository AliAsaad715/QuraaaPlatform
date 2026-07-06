using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Purchases
{
    public class BookPurchaseAggregate : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public Guid BookId { get; private set; }
        public Guid ListingId { get; private set; }
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal TotalPrice => Quantity * UnitPrice;

        private BookPurchaseAggregate() { }

        private BookPurchaseAggregate(
            Guid id,
            Guid userId,
            Guid bookId,
            Guid listingId,
            int quantity,
            decimal unitPrice)
        {
            Id = id;
            UserId = userId;
            BookId = bookId;
            ListingId = listingId;
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

            return new BookPurchaseAggregate(
                Guid.NewGuid(),
                userId,
                bookId,
                listingId,
                quantity,
                unitPrice);
        }
    }
}
