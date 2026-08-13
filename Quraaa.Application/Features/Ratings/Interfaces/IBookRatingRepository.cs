using Quraaa.Application.Features.Ratings.Common;
using Quraaa.Domain.Ratings;

namespace Quraaa.Application.Features.Ratings.Interfaces
{
    public interface IBookRatingRepository
    {
        Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default);
        Task<BookRatingAggregate?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
        Task<BookRatingSummaryResponse> GetSummaryByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
        Task AddAsync(BookRatingAggregate rating, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
