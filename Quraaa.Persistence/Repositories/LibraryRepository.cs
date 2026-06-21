using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Shared.Exceptions;
using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class LibraryRepository : ILibraryRepository
    {
        private readonly ApplicationDbContext _context;

        public LibraryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByUserIdAsync(Guid userId)
        {
            return await _context.Libraries.AnyAsync(l => l.UserId == userId);
        }

        public async Task AddLibraryAsync(LibraryAggregate library)
        {
            await _context.Libraries.AddAsync(library);
        }

        public async Task<(IReadOnlyCollection<LibraryAggregate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Libraries
                .AsNoTracking()
                .Where(l => l.ApprovalStatus == LibraryApprovalStatus.Approved)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim().ToLower();
                query = query.Where(l =>
                    l.LibraryName.ToLower().Contains(normalized) ||
                    l.Location.ToLower().Contains(normalized));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(l => l.LibraryName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateLibraryForUserViolation(ex))
            {
                throw new ApplicationBusinessException(LibraryErrorCodes.DuplicateLibraryForUser);
            }
        }

        private static bool IsDuplicateLibraryForUserViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException postgresException
                && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
                && postgresException.ConstraintName == "IX_Libraries_UserId";
        }
    }
}
