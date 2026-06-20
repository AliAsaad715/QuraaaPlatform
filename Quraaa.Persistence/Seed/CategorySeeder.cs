using Microsoft.EntityFrameworkCore;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed
{
    public static class CategorySeeder
    {
        public static async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
        {
            if (await context.Categories.AnyAsync(cancellationToken))
            {
                return;
            }

            await context.Categories.AddRangeAsync(CategorySeedData.GetSeedCategories(), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}