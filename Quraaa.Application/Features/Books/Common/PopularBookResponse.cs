namespace Quraaa.Application.Features.Books.Common
{
    public record PopularBookResponse(
        Guid BookId,
        string Title,
        string Author,
        string Description,
        string CoverImageUrl,
        Guid? CategoryId,
        string Language,
        string? Isbn,
        long PurchaseCount,
        int RatingCount,
        double? AverageRating,
        int ActiveListingCount);
}
