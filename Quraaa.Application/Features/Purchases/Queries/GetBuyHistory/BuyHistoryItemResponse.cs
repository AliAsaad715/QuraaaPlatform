using Quraaa.Application.Features.Catalog.Common;

namespace Quraaa.Application.Features.Purchases.Queries.GetBuyHistory
{
    public record BuyHistoryItemResponse(
        Guid PurchaseId,
        BookDetails Book,
        int Quantity,
        decimal UnitPrice,
        decimal TotalPrice,
        DateTime PurchasedAt,
        string? PurchasedDigitalAssetUrl
    );
}