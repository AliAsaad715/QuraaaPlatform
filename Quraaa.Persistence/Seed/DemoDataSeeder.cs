using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IPasswordHasher<ApplicationUser> passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken);

        // Serializes demo seeding across API replicas. The transaction-scoped
        // PostgreSQL advisory lock releases automatically on commit/rollback.
        await context.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(718568585);",
            cancellationToken);

        await CategorySeeder.SeedAsync(
            context,
            cancellationToken: cancellationToken,
            requireStableIds: true);
        context.ChangeTracker.Clear();

        await UserSeeder.SeedAsync(
            context,
            userManager,
            roleManager,
            passwordHasher,
            cancellationToken);
        context.ChangeTracker.Clear();

        await DemoProfileSeeder.SeedAsync(context, cancellationToken);
        context.ChangeTracker.Clear();

        await LibrarySeeder.SeedAsync(context, cancellationToken);
        context.ChangeTracker.Clear();

        await UserSeeder.EnsureApprovedLibraryOwnerRolesAsync(
            context,
            userManager,
            roleManager,
            cancellationToken);
        context.ChangeTracker.Clear();

        var demoBookIds = await DemoCatalogSeeder.SeedAsync(context, cancellationToken);
        context.ChangeTracker.Clear();

        await EbookSeeder.SeedAsync(context, cancellationToken);
        context.ChangeTracker.Clear();

        await BookSeeder.SeedAsync(context);
        context.ChangeTracker.Clear();

        await DemoCommerceSeeder.SeedAsync(context, cancellationToken);
        context.ChangeTracker.Clear();

        await DemoEngagementSeeder.SeedAsync(
            context,
            demoBookIds,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
