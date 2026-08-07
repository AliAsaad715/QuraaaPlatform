using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Domain.Marketplace
{
    public class ListingAggregate : AggregateRoot
    {
        public Guid BookId { get; private set; }
        public SellerType SellerType { get; private set; }
        public Guid? LibraryId { get; private set; }
        public Guid? UserId { get; private set; }
        public ListingFormat Format { get; private set; }
        public decimal Price { get; private set; }
        public BookCondition? Condition { get; private set; }

        // The digital file served for this listing. Named "Custom" to distinguish it
        // from BookAggregate's canonical files, since a merchant may need a listing-
        // specific file that differs from the book's catalog-level copy.
        public string? CustomDigitalAssetUrl { get; private set; }
        public int? Stock { get; private set; }
        public ListingStatus Status { get; private set; }

        private ListingAggregate() { }

        private ListingAggregate(
            Guid id,
            Guid bookId,
            ListingFormat format,
            SellerType sellerType,
            Guid? libraryId,
            Guid? userId,
            decimal price,
            BookCondition? condition,
            string? customDigitalAssetUrl,
            int? stock)
        {
            Id = id;
            BookId = bookId;
            Format = format;
            SellerType = sellerType;
            LibraryId = libraryId;
            UserId = userId;
            Price = price;
            Condition = condition;
            CustomDigitalAssetUrl = customDigitalAssetUrl;
            Stock = stock;
            Status = ListingStatus.Active;
        }

        public static ListingAggregate CreateForLibrary(
            Guid id,
            Guid bookId,
            Guid libraryId,
            decimal price,
            BookCondition condition,
            int stock)
        {
            if (price <= 0)
            {
                throw new DomainException("Price must be greater than zero.");
            }

            if (stock <= 0)
            {
                throw new DomainException("Stock must be greater than zero.");
            }

            return new ListingAggregate(
                id, bookId, ListingFormat.Physical, SellerType.Library,
                libraryId, null, price, condition, null, stock);
        }

        public static ListingAggregate CreateDigitalForLibrary(
            Guid id,
            Guid bookId,
            Guid libraryId,
            decimal price,
            string customDigitalAssetUrl)
        {
            if (price <= 0)
            {
                throw new DomainException("Price must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(customDigitalAssetUrl))
            {
                throw new DomainException("A digital asset reference is required for digital listings.");
            }

            return new ListingAggregate(
                id, bookId, ListingFormat.Digital, SellerType.Library,
                libraryId, null, price, null, customDigitalAssetUrl, 1);
        }

        // Users can sell either format (per your note). Physical listings need
        // a condition + stock count; digital listings need an asset reference.
        public static ListingAggregate CreateForUser(
            Guid id,
            Guid bookId,
            Guid userId,
            ListingFormat format,
            decimal price,
            BookCondition? condition = null,
            string? customDigitalAssetUrl = null)
        {
            if (price <= 0)
            {
                throw new DomainException("Price must be greater than zero.");
            }

            if (format == ListingFormat.Physical)
            {
                if (condition is null)
                {
                    throw new DomainException("Condition is required for physical listings.");
                }
            }
            else if (string.IsNullOrWhiteSpace(customDigitalAssetUrl))
            {
                throw new DomainException("A digital asset reference is required for digital listings.");
            }

            return new ListingAggregate(
                id, bookId, format, SellerType.User,
                null, userId, price, condition, customDigitalAssetUrl, 1);
        }

        public void Remove(Guid modifiedBy)
        {
            Status = ListingStatus.Removed;
            UpdateAudit(modifiedBy);
        }

        public void Reactivate(Guid modifiedBy)
        {
            if (Status != ListingStatus.Removed)
            {
                throw new DomainException("Only removed listings can be reactivated.");
            }

            Status = ListingStatus.Active;
            UpdateAudit(modifiedBy);
        }

        public void UpdatePrice(decimal price, Guid modifiedBy)
        {
            if (price <= 0)
            {
                throw new DomainException("Price must be greater than zero.");
            }

            Price = price;
            UpdateAudit(modifiedBy);
        }

        public void DecrementStock(Guid modifiedBy)
        {
            ReserveStock(1, modifiedBy);
        }

        public void ReserveStock(int quantity, Guid modifiedBy)
        {
            if (Format != ListingFormat.Physical || Stock is null)
            {
                throw new DomainException("Only physical listings track stock.");
            }

            if (quantity <= 0)
            {
                throw new DomainException("Reserved stock quantity must be greater than zero.");
            }

            if (Stock < quantity)
            {
                throw new DomainException("Insufficient stock remaining for this listing.");
            }

            Stock -= quantity;

            if (Stock == 0)
            {
                if (SellerType == SellerType.Library)
                {
                    Status = ListingStatus.OutOfStock;
                }
                else if (SellerType == SellerType.User)
                {
                    Status = ListingStatus.Sold;
                }
            }

            UpdateAudit(modifiedBy);
        }

        public void ReleaseReservedStock(int quantity, Guid modifiedBy)
        {
            if (Format != ListingFormat.Physical || Stock is null)
            {
                throw new DomainException("Only physical listings track stock.");
            }

            if (quantity <= 0)
            {
                throw new DomainException("Released stock quantity must be greater than zero.");
            }

            Stock = checked(Stock.Value + quantity);

            if (Status is ListingStatus.OutOfStock or ListingStatus.Sold)
            {
                Status = ListingStatus.Active;
            }

            UpdateAudit(modifiedBy);
        }

        public void UpdateStock(int newStock, Guid modifiedBy)
        {
            if (Format != ListingFormat.Physical)
                throw new DomainException("Only physical listings track stock.");

            if (newStock < 0)
                throw new DomainException("Stock cannot be negative.");

            Stock = newStock;

            // Flip status automatically when stock changes
            if (Stock == 0)
                Status = ListingStatus.OutOfStock;
            else if (Status == ListingStatus.OutOfStock)
                Status = ListingStatus.Active; // restocked

            UpdateAudit(modifiedBy);
        }

        public void UpdateCondition(BookCondition newCondition, Guid modifiedBy)
        {
            if (Format != ListingFormat.Physical)
                throw new DomainException("Only physical listings have a condition.");

            Condition = newCondition;
            UpdateAudit(modifiedBy);
        }

        // Replaces the digital asset for FUTURE purchases only.
        // All existing BookPurchaseAggregate records hold their own
        // PurchasedDigitalAssetUrl snapshot, so buyers who already paid
        // are completely unaffected.
        public void UpdateCustomDigitalAsset(string newUrl, Guid modifiedBy)
        {
            if (Format != ListingFormat.Digital)
                throw new DomainException("Only digital listings have a digital asset.");

            if (string.IsNullOrWhiteSpace(newUrl))
                throw new DomainException("A digital asset URL is required.");

            CustomDigitalAssetUrl = newUrl.Trim();
            UpdateAudit(modifiedBy);
        }
    }
}
