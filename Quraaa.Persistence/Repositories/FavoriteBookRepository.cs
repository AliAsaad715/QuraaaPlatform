using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.FavoriteBooks.Common;
using Quraaa.Application.Features.FavoriteBooks.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Application.Shared.Services;
using Quraaa.Domain.Favorites;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class FavoriteBookRepository : IFavoriteBookRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageUrlFormatter _imageUrlFormatter;

        public FavoriteBookRepository(ApplicationDbContext context, IImageUrlFormatter imageUrlFormatter)
        {
            _context = context;
            _imageUrlFormatter = imageUrlFormatter;
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
            // Materialize first, then project to the response DTO in memory —
            // IImageUrlFormatter.Format can't be translated into SQL.
            var row = await (
                from favorite in _context.FavoriteBooks.AsNoTracking()
                join book in _context.Books.AsNoTracking()
                    on favorite.BookId equals book.Id
                where favorite.UserId == userId
                    && favorite.BookId == bookId
                    && !favorite.IsDeleted
                    && !book.IsDeleted
                select new
                {
                    favorite.Id,
                    favorite.CreationTime,
                    Book = book
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (row is null)
            {
                return null;
            }

            return new FavoriteBookResponse(
                row.Id,
                row.Book.Id,
                row.Book.Title,
                row.Book.Author,
                row.Book.Description,
                _imageUrlFormatter.Format(row.Book.CoverImageUrl),
                row.Book.CategoryId,
                row.Book.Language,
                row.Book.Isbn,
                row.CreationTime);
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

            // Materialize first, then project to the response DTO in memory —
            // IImageUrlFormatter.Format can't be translated into SQL.
            var rows = await query
                .OrderByDescending(favorite => favorite.FavoriteCreationTime)
                .ThenBy(favorite => favorite.Title)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var items = rows
                .Select(favorite => new FavoriteBookResponse(
                    favorite.Id,
                    favorite.BookId,
                    favorite.Title,
                    favorite.Author,
                    favorite.Description,
                    _imageUrlFormatter.Format(favorite.CoverImageUrl),
                    favorite.CategoryId,
                    favorite.Language,
                    favorite.Isbn,
                    favorite.FavoriteCreationTime))
                .ToList();

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
