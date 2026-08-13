using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Comments.Common;
using Quraaa.Application.Features.Comments.Interfaces;
using Quraaa.Domain.Comments;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly ApplicationDbContext _context;

        public CommentRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> BookExistsAsync(Guid bookId, CancellationToken cancellationToken = default)
        {
            return await _context.Books
                .AsNoTracking()
                .AnyAsync(book => book.Id == bookId && !book.IsDeleted, cancellationToken);
        }

        public async Task<CommentAggregate?> GetByIdAsync(Guid commentId, CancellationToken cancellationToken = default)
        {
            return await _context.Comments
                .FirstOrDefaultAsync(comment => comment.Id == commentId && !comment.IsDeleted, cancellationToken);
        }

        public async Task<(IReadOnlyCollection<CommentResponse> Items, int TotalCount)> GetPagedByBookIdAsync(
            Guid bookId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query =
                from comment in _context.Comments.AsNoTracking()
                join user in _context.UsersProfiles.AsNoTracking()
                    on comment.UserId equals user.Id
                where comment.BookId == bookId && !comment.IsDeleted
                select new { comment, user };

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.comment.CreationTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CommentResponse(
                    x.comment.Id,
                    x.comment.BookId,
                    x.comment.UserId,
                    x.user.FirstName + " " + x.user.LastName,
                    x.comment.Content,
                    x.comment.CreationTime,
                    x.comment.LastModificationTime))
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task AddAsync(CommentAggregate comment, CancellationToken cancellationToken = default)
        {
            await _context.Comments.AddAsync(comment, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
