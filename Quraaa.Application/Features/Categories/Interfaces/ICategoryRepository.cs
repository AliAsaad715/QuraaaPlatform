using Quraaa.Domain.Category;

namespace Quraaa.Application.Features.Categories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<CategoryAggregate>> GetByIdsAsync(List<Guid> categoryIds, CancellationToken cancellationToken = default);
        Task<List<CategoryAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<CategoryAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(CategoryAggregate category, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    }
}
