using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Domain.Orders
{
    public sealed record OrderItemSnapshot(
        Guid BookId,
        Guid ListingId,
        SellerType SellerType,
        Guid SellerId,
        ListingFormat Format,
        string BookTitle,
        string BookAuthor,
        string? BookCoverImageUrl,
        string SellerName,
        string? DigitalAssetUrl,
        BookCondition? Condition,
        int Quantity,
        long UnitPriceMinor);
}
