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
        public string? DigitalAssetUrl { get; private set; }
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
            string? digitalAssetUrl,
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
            DigitalAssetUrl = digitalAssetUrl;
            Stock = stock;
            Status = ListingStatus.PendingReview;
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

        // Users can sell either format (per your note). Physical listings need
        // a condition + stock count; digital listings need an asset reference.
        public static ListingAggregate CreateForUser(
            Guid id,
            Guid bookId,
            Guid userId,
            ListingFormat format,
            decimal price,
            BookCondition? condition = null,
            string? digitalAssetUrl = null,
            int? stock = null)
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

                if (stock is null or <= 0)
                {
                    throw new DomainException("Stock must be greater than zero for physical listings.");
                }
            }
            else if (string.IsNullOrWhiteSpace(digitalAssetUrl))
            {
                throw new DomainException("A digital asset reference is required for digital listings.");
            }

            return new ListingAggregate(
                id, bookId, format, SellerType.User,
                null, userId, price, condition, digitalAssetUrl, stock);
        }

        public void Approve(Guid modifiedBy)
        {
            if (Status != ListingStatus.PendingReview)
            {
                throw new DomainException("Only listings pending review can be approved.");
            }

            Status = ListingStatus.Active;
            UpdateAudit(modifiedBy);
        }


        public void Remove(Guid modifiedBy)
        {
            Status = ListingStatus.Removed;
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
            if (Format != ListingFormat.Physical || Stock is null)
            {
                throw new DomainException("Only physical listings track stock.");
            }

            if (Stock <= 0)
            {
                throw new DomainException("No stock remaining for this listing.");
            }

            Stock--;

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
    }
}