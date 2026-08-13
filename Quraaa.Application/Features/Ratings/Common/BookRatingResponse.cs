namespace Quraaa.Application.Features.Ratings.Common
{
    public record BookRatingResponse(
        Guid RatingId,
        Guid BookId,
        Guid UserId,
        int Score,
        DateTime CreatedAt,
        DateTime? ModifiedAt);
}
