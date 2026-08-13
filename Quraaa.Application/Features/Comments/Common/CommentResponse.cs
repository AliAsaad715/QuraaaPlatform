namespace Quraaa.Application.Features.Comments.Common
{
    public record CommentResponse(
        Guid CommentId,
        Guid BookId,
        Guid UserId,
        string CommenterName,
        string Content,
        DateTime CreatedAt,
        DateTime? ModifiedAt);
}
