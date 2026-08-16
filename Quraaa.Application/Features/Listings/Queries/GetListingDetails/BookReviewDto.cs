namespace Quraaa.Application.Features.Listings.Queries.GetListingDetails
{
    public record BookReviewDto(
        int? Rating,
        string Comment,
        string ReviewerName,
        DateTime CreatedAtUtc);
}
