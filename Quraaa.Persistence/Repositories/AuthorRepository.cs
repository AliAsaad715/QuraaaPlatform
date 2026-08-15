using Microsoft.EntityFrameworkCore;
using Npgsql;
using Quraaa.Application.Features.Authors.Interfaces;
using Quraaa.Domain.Author;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class AuthorRepository : IAuthorRepository
    {
        private readonly ApplicationDbContext _context;

        public AuthorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuthorAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // Tracked (not AsNoTracking): Update/Delete handlers mutate the
            // entity returned here and rely on change tracking to persist it.
            return await _context.Authors
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<(IReadOnlyCollection<AuthorAggregate> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Authors.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var normalized = searchTerm.Trim();
                query = query.Where(a => EF.Functions.ILike(a.Name, $"%{normalized}%"));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(a => a.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<AuthorAggregate> FindOrCreateByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            var trimmed = name.Trim();
            var normalized = BookTextNormalizer.Normalize(trimmed);

            // BookTextNormalizer applies Unicode NFKC + diacritic/alef-variant collapsing
            // that cannot be translated to SQL, so candidates are matched in memory.
            // Authors is a small reference table, like Categories, so a full scan is cheap.
            var authors = await _context.Authors.AsNoTracking().ToListAsync(cancellationToken);
            var existing = authors.FirstOrDefault(a => BookTextNormalizer.Normalize(a.Name) == normalized);

            if (existing is not null)
                return existing;

            var author = new AuthorAggregate(Guid.NewGuid(), trimmed, null, null);
            await _context.Authors.AddAsync(author, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return author;
        }

        public async Task<List<AuthorAggregate>> GetByNormalizedNamesAsync(
            IReadOnlyList<string> normalizedNames,
            CancellationToken cancellationToken = default)
        {
            if (normalizedNames.Count == 0)
                return [];

            var lookup = normalizedNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var authors = await _context.Authors.AsNoTracking().ToListAsync(cancellationToken);

            return authors
                .Where(a => lookup.Contains(BookTextNormalizer.Normalize(a.Name)))
                .ToList();
        }

        public async Task AddRangeAsync(IReadOnlyList<AuthorAggregate> authors, CancellationToken cancellationToken = default)
        {
            await _context.Authors.AddRangeAsync(authors, cancellationToken);
        }

        public async Task AddAsync(AuthorAggregate author, CancellationToken cancellationToken = default)
        {
            await _context.Authors.AddAsync(author, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveAsync(AuthorAggregate author, CancellationToken cancellationToken = default)
        {
            _context.Authors.Remove(author);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsForeignKeyViolation(ex))
            {
                _context.Entry(author).State = EntityState.Unchanged;
                throw new ConflictException(
                    "This author cannot be deleted because one or more books still reference it.");
            }
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            _context.SaveChangesAsync(cancellationToken);

        // PostgreSQL error code 23503 = foreign_key_violation
        private static bool IsForeignKeyViolation(DbUpdateException exception) =>
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
    }
}
