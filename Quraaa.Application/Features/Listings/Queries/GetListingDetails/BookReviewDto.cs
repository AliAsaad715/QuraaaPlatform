namespace Quraaa.Application.Features.Listings.Queries.GetListingDetails
{
    // Rating is nullable: Comments and BookRatings are independent aggregates in this domain
    // (a reader can comment without rating, or rate without commenting), so a recent comment
    // doesn't always have a matching star rating from the same reviewer.
    public record BookReviewDto(
        int? Rating,
        string Comment,
        string ReviewerName,
        DateTime CreatedAtUtc);
}
