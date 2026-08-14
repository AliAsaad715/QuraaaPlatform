using Quraaa.Domain.Category;

namespace Quraaa.Application.Features.Categories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<CategoryAggregate>> GetByIdsAsync(List<Guid> categoryIds, CancellationToken cancellationToken = default);
        Task<List<CategoryAggregate>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<CategoryAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task AddAsync(CategoryAggregate category, CancellationToken cancellationToken = default);
        Task RemoveAsync(CategoryAggregate category, CancellationToken cancellationToken = default);
        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns true if another category (any id other than <paramref name="excludingId"/>)
        /// already uses <paramref name="nameAr"/> or <paramref name="nameEn"/>.
        /// </summary>
        Task<bool> ExistsByNameExcludingIdAsync(
            string nameAr,
            string nameEn,
            Guid excludingId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns true if any (non-deleted) book still references this category.
        /// Listings never carry a CategoryId of their own — they reference a book,
        /// so checking books also covers every listing under this category.
        /// </summary>
        Task<bool> HasLinkedBooksAsync(Guid categoryId, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
