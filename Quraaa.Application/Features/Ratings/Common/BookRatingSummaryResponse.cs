namespace Quraaa.Application.Features.Ratings.Common
{
    public record BookRatingSummaryResponse(Guid BookId, double? AverageScore, int TotalCount);
}
