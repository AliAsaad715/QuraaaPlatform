using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.FavoriteBooks.Common;
using Quraaa.Application.Features.FavoriteBooks.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Domain.Favorites;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class FavoriteBookRepository : IFavoriteBookRepository
    {
        private readonly ApplicationDbContext _context;

        public FavoriteBookRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default)
        {
            return await _context.Books
                .AsNoTracking()
                .AnyAsync(book => book.Id == bookId && !book.IsDeleted, cancellationToken);
        }

        public async Task<FavoriteBookResponse?> GetFavoriteAsync(
            Guid userId,
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            return await (
                from favorite in _context.FavoriteBooks.AsNoTracking()
                join book in _context.Books.AsNoTracking()
                    on favorite.BookId equals book.Id
                where favorite.UserId == userId
                    && favorite.BookId == bookId
                    && !favorite.IsDeleted
                    && !book.IsDeleted
                select new FavoriteBookResponse(
                    favorite.Id,
                    book.Id,
                    book.Title,
                    book.Author,
                    book.Description,
                    book.CoverImageUrl,
                    book.CategoryId,
                    book.Language,
                    book.Isbn,
                    favorite.CreationTime))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<(IReadOnlyCollection<FavoriteBookResponse> Items, int TotalCount)> GetPagedAsync(
            Guid userId,
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            var query =
                from favorite in _context.FavoriteBooks.AsNoTracking()
                join book in _context.Books.AsNoTracking()
                    on favorite.BookId equals book.Id
                where favorite.UserId == userId
                    && !favorite.IsDeleted
                    && !book.IsDeleted
                select new
                {
                    favorite.Id,
                    FavoriteCreationTime = favorite.CreationTime,
                    BookId = book.Id,
                    book.Title,
                    book.Author,
                    book.Description,
                    book.CoverImageUrl,
                    book.CategoryId,
                    book.Language,
                    book.Isbn
                };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(favorite =>
                    EF.Functions.ILike(favorite.Title, $"%{normalized}%") ||
                    EF.Functions.ILike(favorite.Author, $"%{normalized}%"));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(favorite => favorite.FavoriteCreationTime)
                .ThenBy(favorite => favorite.Title)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(favorite => new FavoriteBookResponse(
                    favorite.Id,
                    favorite.BookId,
                    favorite.Title,
                    favorite.Author,
                    favorite.Description,
                    favorite.CoverImageUrl,
                    favorite.CategoryId,
                    favorite.Language,
                    favorite.Isbn,
                    favorite.FavoriteCreationTime))
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task AddAsync(FavoriteBookAggregate favoriteBook, CancellationToken cancellationToken = default)
        {
            await _context.FavoriteBooks.AddAsync(favoriteBook, cancellationToken);
        }

        public async Task<bool> RemoveAsync(
            Guid userId,
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            var favoriteBook = await _context.FavoriteBooks
                .FirstOrDefaultAsync(
                    favorite => favorite.UserId == userId
                        && favorite.BookId == bookId
                        && !favorite.IsDeleted,
                    cancellationToken);

            if (favoriteBook is null)
            {
                return false;
            }

            favoriteBook.Delete(userId);
            return true;
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsDuplicateFavoriteBookViolation(ex))
            {
                foreach (var entry in ex.Entries.Where(entry => entry.Entity is FavoriteBookAggregate))
                {
                    entry.State = EntityState.Detached;
                }

                throw new ApplicationBusinessException(FavoriteBookErrorCodes.DuplicateFavoriteBook);
            }
        }

        private static bool IsDuplicateFavoriteBookViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_FavoriteBooks_UserId_BookId"
            };
    }
}
