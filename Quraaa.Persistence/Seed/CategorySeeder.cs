using Microsoft.EntityFrameworkCore;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed
{
    public static class CategorySeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken = default,
            bool requireStableIds = false)
        {
            var seedCategories = CategorySeedData.GetSeedCategories();
            var seedIds = seedCategories.Select(category => category.Id).ToArray();
            var seedCodes = seedCategories.Select(category => category.Code).ToArray();

            var existingCategories = await context.Categories
                .IgnoreQueryFilters()
                .Where(category =>
                    seedIds.Contains(category.Id) ||
                    seedCodes.Contains(category.Code))
                .Select(category => new { category.Id, category.Code })
                .ToListAsync(cancellationToken);

            var missingCategories = new List<Quraaa.Domain.Category.CategoryAggregate>();

            foreach (var seedCategory in seedCategories)
            {
                var existingById = existingCategories.FirstOrDefault(category =>
                    category.Id == seedCategory.Id);
                var existingByCode = existingCategories.FirstOrDefault(category =>
                    string.Equals(
                        category.Code,
                        seedCategory.Code,
                        StringComparison.Ordinal));

                if (existingById is not null &&
                    !string.Equals(
                        existingById.Code,
                        seedCategory.Code,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Seed category id {seedCategory.Id} belongs to code " +
                        $"'{existingById.Code}', not '{seedCategory.Code}'.");
                }

                if (existingByCode is not null && existingByCode.Id != seedCategory.Id)
                {
                    if (requireStableIds)
                    {
                        throw new InvalidOperationException(
                            $"Demo category code '{seedCategory.Code}' belongs to id " +
                            $"{existingByCode.Id}, not {seedCategory.Id}.");
                    }

                    continue;
                }

                if (existingById is null && existingByCode is null)
                {
                    missingCategories.Add(seedCategory);
                }
            }

            if (missingCategories.Count == 0)
            {
                return;
            }

            await context.Categories.AddRangeAsync(missingCategories, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
