using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Ratings.Common;
using Quraaa.Application.Features.Ratings.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Domain.Ratings;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookRatingRepository : IBookRatingRepository
    {
        private readonly ApplicationDbContext _context;

        public BookRatingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default)
        {
            return await _context.Books
                .AsNoTracking()
                .AnyAsync(book => book.Id == bookId && !book.IsDeleted, cancellationToken);
        }

        public async Task<BookRatingAggregate?> GetByUserAndBookAsync(
            Guid userId,
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            return await _context.BookRatings
                .FirstOrDefaultAsync(
                    rating => rating.UserId == userId && rating.BookId == bookId && !rating.IsDeleted,
                    cancellationToken);
        }

        public async Task<BookRatingSummaryResponse> GetSummaryByBookIdAsync(
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            var query = _context.BookRatings
                .AsNoTracking()
                .Where(rating => rating.BookId == bookId && !rating.IsDeleted);

            var totalCount = await query.CountAsync(cancellationToken);
            double? averageScore = totalCount == 0
                ? null
                : await query.AverageAsync(rating => (double)rating.RatingValue, cancellationToken);

            return new BookRatingSummaryResponse(bookId, averageScore, totalCount);
        }

        public async Task AddAsync(BookRatingAggregate rating, CancellationToken cancellationToken = default)
        {
            await _context.BookRatings.AddAsync(rating, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsDuplicateRatingViolation(ex))
            {
                foreach (var entry in ex.Entries.Where(entry => entry.Entity is BookRatingAggregate))
                {
                    entry.State = EntityState.Detached;
                }

                throw new ApplicationBusinessException(RatingErrorCodes.DuplicateRating);
            }
        }

        private static bool IsDuplicateRatingViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_BookRatings_UserId_BookId"
            };
    }
}
