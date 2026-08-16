namespace Quraaa.Application.Features.Purchases.Common
{
    /// <summary>Projection used to authorize and prepare a purchase's digital asset stream.</summary>
    public sealed record PurchaseDigitalAssetInfo(
        Guid UserId,
        string? PurchasedDigitalAssetUrl,
        string BookTitle);
}
