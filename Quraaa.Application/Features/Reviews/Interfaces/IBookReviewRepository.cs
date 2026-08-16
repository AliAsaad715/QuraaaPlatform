using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Domain.Reviews;

namespace Quraaa.Application.Features.Reviews.Interfaces
{
    public interface IBookReviewRepository
    {
        Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default);
        Task<BookReviewAggregate?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
        Task<(IReadOnlyCollection<BookReviewResponse> Items, int TotalCount)> GetPagedByBookIdAsync(
            Guid bookId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);
        Task AddAsync(BookReviewAggregate review, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
