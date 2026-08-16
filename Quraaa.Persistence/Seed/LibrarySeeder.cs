using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed;

public static class LibrarySeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var passwordHasher = new PasswordHasher<LibraryAggregate>();

        var ownerPhoneNumbers = UserSeeder.LibraryOwnerPhoneNumbers.ToArray();
        var owners = await context.UsersProfiles
            .Where(user => ownerPhoneNumbers.Contains(user.PhoneNumber))
            .Select(user => new { user.Id, user.PhoneNumber })
            .ToListAsync(cancellationToken);

        var ownerIdByPhone = owners.ToDictionary(
            owner => owner.PhoneNumber,
            owner => owner.Id,
            StringComparer.Ordinal);

        if (ownerIdByPhone.Count != UserSeeder.LibraryOwnerCount)
        {
            throw new InvalidOperationException(
                "One or more demo library-owner profiles are missing.");
        }

        var targetIds = Enumerable.Range(1, UserSeeder.LibraryOwnerCount)
            .Select(DemoSeedData.GetLibraryId)
            .ToArray();
        var targetEmails = Enumerable.Range(1, UserSeeder.LibraryOwnerCount)
            .Select(GetEmail)
            .ToArray();

        var existingLibraries = await context.Libraries
            .Where(library =>
                targetIds.Contains(library.Id) ||
                targetEmails.Contains(library.Email))
            .ToListAsync(cancellationToken);

        var byId = existingLibraries.ToDictionary(library => library.Id);
        var byEmail = existingLibraries.ToDictionary(
            library => library.Email,
            StringComparer.OrdinalIgnoreCase);

        var moderatorId = await context.UsersProfiles
            .Where(user => user.PhoneNumber == DemoSeedData.SuperAdminPhoneNumber)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? DemoSeedData.SuperAdminUserId;

        for (var index = 1; index <= UserSeeder.LibraryOwnerCount; index++)
        {
            var ownerPhone = UserSeeder.LibraryOwnerPhoneNumbers[index - 1];
            var ownerId = ownerIdByPhone[ownerPhone];

            var id = DemoSeedData.GetLibraryId(index);
            var email = GetEmail(index);

            byId.TryGetValue(id, out var existingById);
            byEmail.TryGetValue(email, out var existingByEmail);

            if (existingById is not null &&
                existingByEmail is not null &&
                existingById.Id != existingByEmail.Id)
            {
                throw new InvalidOperationException(
                    $"Demo library seed collision for {email} and {id}.");
            }

            var library = existingByEmail ?? existingById;
            if (library is not null)
            {
                if (library.UserId != ownerId ||
                    !string.Equals(library.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Demo library seed key {email} belongs to a different record.");
                }

                var passwordVerification = string.IsNullOrWhiteSpace(library.PasswordHash)
                    ? PasswordVerificationResult.Failed
                    : passwordHasher.VerifyHashedPassword(
                        library,
                        library.PasswordHash,
                        DemoSeedData.LibraryPassword);
                if (passwordVerification != PasswordVerificationResult.Success)
                {
                    library.SetPasswordHash(
                        passwordHasher.HashPassword(
                            library,
                            DemoSeedData.LibraryPassword),
                        moderatorId);
                }

                ConfigureShowcaseWallet(library, index, moderatorId);
                ApplyCreationTimeline(context, library, index);
                continue;
            }

            library = new LibraryAggregate(
                id,
                GetName(index),
                GetLocation(index),
                "https://images.unsplash.com/photo-1507842217343-583bb7270b66?w=500&q=80",
                "https://images.unsplash.com/photo-1521587760476-6c12a4b040da?w=1200&q=80",
                email,
                ownerId,
                passwordHasher.HashPassword(
                    null!,
                    DemoSeedData.LibraryPassword));

            ApplyApprovalScenario(library, index, moderatorId);
            ConfigureShowcaseWallet(library, index, moderatorId);

            await context.Libraries.AddAsync(library, cancellationToken);
            ApplyCreationTimeline(context, library, index);
            byId[id] = library;
            byEmail[email] = library;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyApprovalScenario(
        LibraryAggregate library,
        int index,
        Guid moderatorId)
    {
        // Dedicated scenario records use new natural keys so an older demo
        // database is upgraded safely without rewriting prior approved rows.
        if (index == 102)
        {
            return;
        }

        library.VerifyEmail(DateTime.UtcNow.AddDays(-(230 - index)));

        if (index == 101)
        {
            library.Reject(moderatorId);
            return;
        }

        // Every fourth library remains in the verified admin-review queue.
        if (index % 4 != 0)
        {
            library.Approve(moderatorId);
        }
    }

    private static void ConfigureShowcaseWallet(
        LibraryAggregate library,
        int index,
        Guid moderatorId)
    {
        if (library.ApprovalStatus != LibraryApprovalStatus.Approved)
        {
            return;
        }

        switch (index)
        {
            case 1:
                library.ConnectStripeWallet(
                    "acct_demo_library_active_0001",
                    DateTime.UtcNow.AddDays(-20),
                    moderatorId);
                library.SetProfitSharePercent(70m, moderatorId);
                break;
            case 2:
                library.ConnectStripeWallet(
                    "acct_demo_library_onboarding_0002",
                    activatedAtUtc: null,
                    moderatorId);
                library.SetProfitSharePercent(60m, moderatorId);
                break;
            case 3:
                library.SetProfitSharePercent(55m, moderatorId);
                break;
        }
    }

    private static void ApplyCreationTimeline(
        ApplicationDbContext context,
        LibraryAggregate library,
        int index)
    {
        var creationTime = DateTime.UtcNow.AddDays(-(240 - index));
        var lastModificationTime = index switch
        {
            1 => DateTime.UtcNow.AddDays(-20),
            2 => DateTime.UtcNow.AddDays(-15),
            3 => DateTime.UtcNow.AddDays(-30),
            _ => library.EmailVerifiedAtUtc?.AddDays(1) ?? creationTime.AddDays(7),
        };

        context.Entry(library)
            .Property(nameof(LibraryAggregate.CreationTime))
            .CurrentValue = creationTime;
        context.Entry(library)
            .Property(nameof(LibraryAggregate.LastModificationTime))
            .CurrentValue = lastModificationTime;
    }

    private static string GetEmail(int index) => $"info.lib{index}@quraaa.com";

    private static string GetName(int index) => index switch
    {
        1 => "Dar Al-Hikma Central Library",
        2 => "Al-Qalam Book House",
        3 => "Horizon Knowledge Library",
        4 => "Al-Amal Community Library",
        101 => "Heritage Books - Rejected Demo",
        102 => "Future Pages - Email Verification Demo",
        _ => $"Quraaa Library {index:000}",
    };

    private static string GetLocation(int index) => index switch
    {
        1 => "Al-Mutanabbi Street, Baghdad",
        2 => "University District, Baghdad",
        3 => "Al-Ashar, Basra",
        4 => "Ankawa, Erbil",
        101 => "Old City, Mosul",
        102 => "Al-Karrada, Baghdad",
        _ => $"Demo District {((index - 1) % 10) + 1}, Branch {index:000}",
    };
}
