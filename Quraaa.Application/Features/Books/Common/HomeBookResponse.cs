using Quraaa.Application.Features.Categories.Common;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Application.Features.Books.Common
{
    public record HomeBookResponse(
        Guid ListingId,
        string Title,
        string? AuthorName,
        string CoverImageUrl,
        CategoryResponse? Category,
        ListingFormat Format,
        decimal StartingPrice,
        bool IsFree,
        int SellersCount,
        double? AverageRating,
        int RatingsCount);
}
