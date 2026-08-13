using Quraaa.Application.Features.Comments.Common;
using Quraaa.Domain.Comments;

namespace Quraaa.Application.Features.Comments.Interfaces
{
    public interface ICommentRepository
    {
        Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default);
        Task<CommentAggregate?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default);
        Task<(IReadOnlyCollection<CommentResponse> Items, int TotalCount)> GetPagedByBookIdAsync(
            Guid bookId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task AddAsync(CommentAggregate comment, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
