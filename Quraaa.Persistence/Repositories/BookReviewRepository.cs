using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Reviews.Common;
using Quraaa.Application.Features.Reviews.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Reviews;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookReviewRepository : IBookReviewRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public BookReviewRepository(ApplicationDbContext context, IImageUrlFormatter imageUrlFormatter)
        {
            _context = context;
            _imageUrlFormatter = imageUrlFormatter;
        }

        public async Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            await _context.Books.AsNoTracking().AnyAsync(book => book.Id == bookId && !book.IsDeleted, cancellationToken);

        public async Task<BookReviewAggregate?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default) =>
            await _context.BookReviews
                .FirstOrDefaultAsync(review => review.UserId == userId && review.BookId == bookId && !review.IsDeleted, cancellationToken);

        public async Task<(IReadOnlyCollection<BookReviewResponse> Items, int TotalCount)> GetPagedByBookIdAsync(
            Guid bookId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.BookReviews
                .AsNoTracking()
                .Where(review => review.BookId == bookId && !review.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);

            // Materialize first, then project to the response DTO in memory —
            // IImageUrlFormatter.Format can't be translated into SQL.
            var rows = await query
                .OrderByDescending(review => review.CreationTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Join(
                    _context.UsersProfiles.AsNoTracking(),
                    review => review.UserId,
                    user => user.Id,
                    (review, user) => new { Review = review, User = user })
                .ToListAsync(cancellationToken);

            var items = rows
                .Select(x => new BookReviewResponse(
                    x.Review.Id,
                    x.Review.UserId,
                    $"{x.User.FirstName} {x.User.LastName}",
                    _imageUrlFormatter.Format(x.User.ProfileImageUrl),
                    x.Review.Score,
                    x.Review.Content,
                    x.Review.CreationTime))
                .ToList();

            return (items, totalCount);
        }

        public async Task AddAsync(BookReviewAggregate review, CancellationToken cancellationToken = default) =>
            await _context.BookReviews.AddAsync(review, cancellationToken);

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsDuplicateReviewViolation(ex))
            {
                foreach (var entry in ex.Entries.Where(entry => entry.Entity is BookReviewAggregate))
                {
                    entry.State = EntityState.Detached;
                }

                throw new ApplicationBusinessException(ReviewErrorCodes.DuplicateReview);
            }
        }

        private static bool IsDuplicateReviewViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_BookReviews_UserId_BookId"
            };
    }
}
