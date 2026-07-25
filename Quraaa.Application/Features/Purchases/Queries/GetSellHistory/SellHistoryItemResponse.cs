using Quraaa.Application.Features.Catalog.Common;

namespace Quraaa.Application.Features.Purchases.Queries.GetSellHistory
{
    public record SellHistoryItemResponse(
        Guid PurchaseId,
        BookDetails Book,
        int Quantity,
        decimal UnitPrice,
        decimal TotalEarned,
        Guid BuyerUserId,
        DateTime SoldAt
    );
}