using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Cart.Entities
{
    public class CartItem : Entity
    {
        public Guid CartId { get; private set; }
        public Guid ListingId { get; private set; }
        public int Quantity { get; private set; }
        public decimal UnitPriceSnapshot { get; private set; }

        private CartItem() { }

        private CartItem(Guid id, Guid cartId, Guid listingId, int quantity, decimal unitPriceSnapshot)
        {
            Id = id;
            CartId = cartId;
            ListingId = listingId;
            Quantity = quantity;
            UnitPriceSnapshot = unitPriceSnapshot;
        }

        public static CartItem Create(Guid cartId, Guid listingId, int quantity, decimal unitPriceSnapshot)
        {
            if (cartId == Guid.Empty)
            {
                throw new DomainException("Cart id is required.");
            }

            if (listingId == Guid.Empty)
            {
                throw new DomainException("Listing id is required.");
            }

            if (quantity <= 0)
            {
                throw new DomainException("Quantity must be greater than zero.");
            }

            if (unitPriceSnapshot < 0)
            {
                throw new DomainException("Unit price cannot be negative.");
            }

            return new CartItem(Guid.NewGuid(), cartId, listingId, quantity, unitPriceSnapshot);
        }

        public void IncreaseQuantity(int quantity)
        {
            if (quantity <= 0)
            {
                throw new DomainException("Quantity must be greater than zero.");
            }

            Quantity += quantity;
        }

        public void SetQuantity(int quantity)
        {
            if (quantity <= 0)
            {
                throw new DomainException("Quantity must be greater than zero.");
            }

            Quantity = quantity;
        }
    }
}
