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
            return await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task AddAsync(CategoryAggregate category, CancellationToken cancellationToken = default)
        {
            await _context.Categories.AddAsync(category, cancellationToken);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var normalized = code.Trim().ToLower();
            return await _context.Categories.AnyAsync(c => c.Code.ToLower() == normalized, cancellationToken);
        }
    }
}
