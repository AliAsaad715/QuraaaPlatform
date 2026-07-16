using Quraaa.Domain.Cart.Entities;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;

namespace Quraaa.Domain.Cart
{
    public class CartAggregate : AggregateRoot
    {
        public Guid UserId { get; private set; }
        public CartStatus Status { get; private set; }
        public string? StripeCheckoutSessionId { get; private set; }
        public string? StripePaymentIntentId { get; private set; }

        private readonly List<CartItem> _items = new();
        public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

        public decimal TotalAmount => _items.Sum(x => x.Quantity * x.UnitPriceSnapshot);

        private CartAggregate() { }

        private CartAggregate(Guid id, Guid userId)
        {
            Id = id;
            UserId = userId;
            Status = CartStatus.Active;
        }

        public static CartAggregate Create(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new DomainException("User id is required.");
            }

            return new CartAggregate(Guid.NewGuid(), userId);
        }

        public void AddOrIncreaseItem(Guid listingId, int quantity, decimal unitPriceSnapshot)
        {
            EnsureModifiable();

            var existing = _items.FirstOrDefault(x => x.ListingId == listingId);
            if (existing is null)
            {
                _items.Add(CartItem.Create(Id, listingId, quantity, unitPriceSnapshot));
                return;
            }

            existing.IncreaseQuantity(quantity);
            UpdateModificationTime();
        }

        public void UpdateItemQuantity(Guid listingId, int quantity)
        {
            EnsureModifiable();

            var item = _items.FirstOrDefault(x => x.ListingId == listingId)
                ?? throw new NotFoundException("Cart item not found.");

            item.SetQuantity(quantity);
            UpdateModificationTime();
        }

        public void RemoveItem(Guid listingId)
        {
            EnsureModifiable();
            _items.RemoveAll(x => x.ListingId == listingId);
            UpdateModificationTime();
        }

        public void Clear()
        {
            EnsureModifiable();
            _items.Clear();
            UpdateModificationTime();
        }

        public void MarkPendingPayment(string stripeCheckoutSessionId)
        {
            if (string.IsNullOrWhiteSpace(stripeCheckoutSessionId))
            {
                throw new DomainException("Stripe checkout session id is required.");
            }

            Status = CartStatus.PendingPayment;
            StripeCheckoutSessionId = stripeCheckoutSessionId;
            UpdateModificationTime();
        }

        public void MarkPaid(string? stripePaymentIntentId = null)
        {
            Status = CartStatus.Paid;
            StripePaymentIntentId = stripePaymentIntentId;
            UpdateModificationTime();
        }

        public void EnsureModifiable()
        {
            if (Status != CartStatus.Active)
            {
                throw new ConflictException("Cart is locked during payment processing.");
            }
        }
    }
}
