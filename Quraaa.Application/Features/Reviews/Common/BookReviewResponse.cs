namespace Quraaa.Application.Features.Reviews.Common
{
    public record BookReviewResponse(
        Guid Id,
        Guid UserId,
        string UserName,
        string UserAvatarUrl,
        int Score,
        string Content,
        DateTime CreationTimeUtc);
}
