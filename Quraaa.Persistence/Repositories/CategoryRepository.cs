using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Categories.Interfaces;
using Quraaa.Domain.Category;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public CategoryRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CategoryAggregate>> GetByIdsAsync(List<Guid> categoryIds, CancellationToken cancellationToken = default)
        {
            if (categoryIds == null || !categoryIds.Any())
                return new List<CategoryAggregate>();

            return await _context.Categories
                .AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id))
                .ToListAsync(cancellationToken);
        }

        public async Task<List<CategoryAggregate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Categories
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<CategoryAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // Tracked (not AsNoTracking): Update/Delete handlers mutate the
            // entity returned here and rely on change tracking to persist it.
            return await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task AddAsync(CategoryAggregate category, CancellationToken cancellationToken = default)
        {
            await _context.Categories.AddAsync(category, cancellationToken);
        }

        public Task RemoveAsync(CategoryAggregate category, CancellationToken cancellationToken = default)
        {
            _context.Categories.Remove(category);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var normalized = code.Trim().ToLower();
            return await _context.Categories.AnyAsync(c => c.Code.ToLower() == normalized, cancellationToken);
        }

        public async Task<bool> ExistsByNameExcludingIdAsync(
            string nameAr,
            string nameEn,
            Guid excludingId,
            CancellationToken cancellationToken = default)
        {
            var normalizedAr = nameAr.Trim().ToLower();
            var normalizedEn = nameEn.Trim().ToLower();

            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(
                    c => c.Id != excludingId &&
                        (c.NameAr.ToLower() == normalizedAr || c.NameEn.ToLower() == normalizedEn),
                    cancellationToken);
        }

        public async Task<bool> HasLinkedBooksAsync(Guid categoryId, CancellationToken cancellationToken = default)
        {
            return await _context.Books
                .AsNoTracking()
                .AnyAsync(b => b.CategoryId == categoryId && !b.IsDeleted, cancellationToken);
        }
    }
}
