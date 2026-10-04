using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Domain.Catalog;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class BookVersionRepository : IBookVersionRepository
    {
        private readonly ApplicationDbContext _context;

        public BookVersionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<BookAggregate?> GetBookForUpdateAsync(
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            // Tracked: callers mutate the book's details or moderation state.
            // IgnoreQueryFilters, because a withheld book is exactly the one a
            // moderator needs to load in order to inspect or restore it.
            return await _context.Books
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(book => book.Id == bookId && !book.IsDeleted, cancellationToken);
        }

        public async Task<BookVersion?> GetVersionAsync(
            Guid bookId,
            int versionNumber,
            CancellationToken cancellationToken = default)
        {
            return await _context.BookVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    version => version.BookId == bookId
                        && version.VersionNumber == versionNumber,
                    cancellationToken);
        }

        public async Task<IReadOnlyCollection<BookVersion>> GetVersionsAsync(
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            return await _context.BookVersions
                .AsNoTracking()
                .Where(version => version.BookId == bookId)
                .OrderBy(version => version.VersionNumber)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> HasAnyVersionAsync(
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            return await _context.BookVersions
                .AsNoTracking()
                .AnyAsync(version => version.BookId == bookId, cancellationToken);
        }

        public async Task AddAsync(
            BookVersion version,
            CancellationToken cancellationToken = default)
        {
            await _context.BookVersions.AddAsync(version, cancellationToken);
        }
    }
}
