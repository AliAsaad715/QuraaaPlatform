namespace Quraaa.Application.Features.Purchases.Common
{
    public sealed record PurchaseDigitalBookResponse(
        Guid PurchaseId,
        Guid BookId,
        string PurchasedDigitalAssetUrl,
        decimal UnitPrice,
        DateTime PurchasedAt);
}
