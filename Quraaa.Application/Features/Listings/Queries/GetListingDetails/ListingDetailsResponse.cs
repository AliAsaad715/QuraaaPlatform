using Quraaa.Domain.Catalog.Enums;
using Quraaa.Domain.Marketplace.Enums;

namespace Quraaa.Application.Features.Listings.Queries.GetListingDetails
{
    public record ListingDetailsResponse(
        Guid Id,
        Guid BookId,
        string Title,
        string CoverImageUrl,
        ListingFormat Format,
        BookCondition? Condition,
        Language Language,
        string Publisher,
        string Writer,
        int Version,
        decimal Price,
        List<string> PreviewImageUrls,
        double AverageRating,
        int TotalReviewsCount,
        List<BookReviewDto> RecentReviews);
}
