using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed
{
    public static class UserSeeder
    {
        public const int LibraryOwnerCount = 102;

        public static IReadOnlyList<string> LibraryOwnerPhoneNumbers { get; } =
            Enumerable.Range(1, LibraryOwnerCount)
                .Select(GetLibraryOwnerPhoneNumber)
                .ToArray();

        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IPasswordHasher<ApplicationUser> passwordHasher,
            CancellationToken cancellationToken = default)
        {
            var userRole = Role.User.ToString();
            var libraryOwnerRole = Role.LibraryOwner.ToString();

            await EnsureRoleAsync(roleManager, userRole);
            await EnsureRoleAsync(roleManager, libraryOwnerRole);

            foreach (var demoUser in DemoSeedData.Users)
            {
                await EnsureUserAsync(
                    context,
                    userManager,
                    roleManager,
                    passwordHasher,
                    demoUser.Id,
                    demoUser.PhoneNumber,
                    demoUser.Password,
                    demoUser.FirstName,
                    demoUser.LastName,
                    demoUser.Gender,
                    demoUser.DomainRole,
                    demoUser.IdentityRoles,
                    cancellationToken);
            }

            var ownerIndex = 1;
            foreach (var phoneNumber in LibraryOwnerPhoneNumbers)
            {
                await EnsureUserAsync(
                    context,
                    userManager,
                    roleManager,
                    passwordHasher,
                    DemoSeedData.GetLibraryOwnerUserId(ownerIndex),
                    phoneNumber,
                    DemoSeedData.UserPassword,
                    firstName: "Library",
                    lastName: $"Owner {ownerIndex:000}",
                    gender: ownerIndex % 2 == 0 ? Gender.Female : Gender.Male,
                    domainRole: Role.User,
                    identityRoles: new[] { userRole },
                    cancellationToken);

                ownerIndex++;
            }

            await ApplyDemoCreationTimelineAsync(context, cancellationToken);
        }

        private static string GetLibraryOwnerPhoneNumber(int index)
        {
            return $"+963930000{index:000}";
        }

        private static async Task EnsureRoleAsync(
            RoleManager<IdentityRole<Guid>> roleManager,
            string role)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create role {role}: {errors}");
                }
            }
        }

        public static async Task EnsureApprovedLibraryOwnerRolesAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            CancellationToken cancellationToken)
        {
            var userRole = Role.User.ToString();
            var libraryOwnerRole = Role.LibraryOwner.ToString();

            await EnsureRoleAsync(roleManager, userRole);
            await EnsureRoleAsync(roleManager, libraryOwnerRole);

            var demoOwnerUserIds = await context.UsersProfiles
                .AsNoTracking()
                .Where(user => LibraryOwnerPhoneNumbers.Contains(user.PhoneNumber))
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);

            var ownerUserIds = await context.Libraries
                .AsNoTracking()
                .Where(library =>
                    library.ApprovalStatus == LibraryApprovalStatus.Approved &&
                    demoOwnerUserIds.Contains(library.UserId))
                .Select(library => library.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (ownerUserIds.Count == 0)
            {
                return;
            }

            var ownerProfiles = await context.UsersProfiles
                .Where(user => ownerUserIds.Contains(user.Id))
                .ToListAsync(cancellationToken);

            var profilesChanged = false;
            foreach (var ownerProfile in ownerProfiles)
            {
                if (ownerProfile.Role == Role.LibraryOwner)
                {
                    continue;
                }

                ownerProfile.BecomeLibraryOwner(ownerProfile.Id);
                profilesChanged = true;
            }

            if (profilesChanged)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            foreach (var ownerUserId in ownerUserIds)
            {
                var applicationUser = await userManager.FindByIdAsync(ownerUserId.ToString());
                if (applicationUser is null)
                {
                    continue;
                }

                await EnsureIdentityRoleAsync(userManager, roleManager, applicationUser, userRole);
                await EnsureIdentityRoleAsync(userManager, roleManager, applicationUser, libraryOwnerRole);
            }
        }

        private static async Task EnsureUserAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IPasswordHasher<ApplicationUser> passwordHasher,
            Guid preferredId,
            string phoneNumber,
            string password,
            string firstName,
            string lastName,
            Gender gender,
            Role domainRole,
            IReadOnlyCollection<string> identityRoles,
            CancellationToken cancellationToken)
        {
            var applicationUser = await userManager.FindByNameAsync(phoneNumber);
            var existingProfile = applicationUser is null
                ? null
                : await context.UsersProfiles.FirstOrDefaultAsync(
                    user => user.Id == applicationUser.Id,
                    cancellationToken);

            if (applicationUser is not null)
            {
                var hasExpectedIdentityKeys =
                    string.Equals(
                        applicationUser.UserName,
                        phoneNumber,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        applicationUser.PhoneNumber,
                        phoneNumber,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        applicationUser.Email,
                        $"{phoneNumber}@quraaa.com",
                        StringComparison.OrdinalIgnoreCase);

                var isRecognizedLegacySeed =
                    applicationUser.Id != preferredId &&
                    IsRecognizedLegacySeedProfile(
                        existingProfile,
                        phoneNumber,
                        firstName,
                        lastName);

                if (!hasExpectedIdentityKeys ||
                    (applicationUser.Id != preferredId && !isRecognizedLegacySeed))
                {
                    throw new InvalidOperationException(
                        $"Demo user seed collision for {phoneNumber}. " +
                        "The matching account is not a recognized demo record.");
                }

                var existingRoles = await userManager.GetRolesAsync(applicationUser);
                var allowedRoles = identityRoles
                    .Concat(IsLibraryOwnerPhoneNumber(phoneNumber)
                        ? [Role.LibraryOwner.ToString()]
                        : [])
                    .ToHashSet(StringComparer.Ordinal);
                var unexpectedRoles = existingRoles
                    .Where(role => !allowedRoles.Contains(role))
                    .ToArray();

                if (unexpectedRoles.Length > 0)
                {
                    throw new InvalidOperationException(
                        $"Demo user {phoneNumber} has unexpected Identity roles: " +
                        string.Join(", ", unexpectedRoles));
                }

                if (existingProfile is not null &&
                    !HasCompatibleDomainRole(existingProfile.Role, domainRole, phoneNumber))
                {
                    throw new InvalidOperationException(
                        $"Demo user {phoneNumber} has unexpected domain role " +
                        $"{existingProfile.Role}.");
                }
            }

            if (applicationUser is null)
            {
                var idOwner = await userManager.FindByIdAsync(preferredId.ToString());
                if (idOwner is not null)
                {
                    throw new InvalidOperationException(
                        $"Demo user id {preferredId} already belongs to " +
                        $"{idOwner.UserName ?? "another account"}.");
                }

                applicationUser = new ApplicationUser
                {
                    Id = preferredId,
                    UserName = phoneNumber,
                    PhoneNumber = phoneNumber,
                    Email = $"{phoneNumber}@quraaa.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                };

                var identityResult = await userManager.CreateAsync(applicationUser, password);
                if (!identityResult.Succeeded)
                {
                    var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to seed user account {phoneNumber}: {errors}");
                }
            }
            else
            {
                var identityChanged = false;
                if (!applicationUser.PhoneNumberConfirmed || !applicationUser.EmailConfirmed)
                {
                    applicationUser.PhoneNumberConfirmed = true;
                    applicationUser.EmailConfirmed = true;
                    identityChanged = true;
                }

                if (identityChanged)
                {
                    var updateResult = await userManager.UpdateAsync(applicationUser);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException(
                            $"Failed to confirm seeded user account {phoneNumber}: {errors}");
                    }
                }

                if (!await userManager.CheckPasswordAsync(applicationUser, password))
                {
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(applicationUser);
                    applicationUser.RefreshToken = null;
                    applicationUser.RefreshTokenExpiryTime = DateTime.UtcNow;
                    applicationUser.RefreshTokenFamilyId = null;

                    var resetResult = await userManager.ResetPasswordAsync(
                        applicationUser,
                        resetToken,
                        password);
                    if (!resetResult.Succeeded)
                    {
                        var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException(
                            $"Failed to reset demo user password {phoneNumber}: {errors}");
                    }
                }
            }

            foreach (var identityRole in identityRoles.Distinct())
            {
                await EnsureIdentityRoleAsync(userManager, roleManager, applicationUser, identityRole);
            }

            if (existingProfile is not null)
            {
                if (!string.IsNullOrWhiteSpace(applicationUser.PasswordHash) &&
                    existingProfile.PasswordHash != applicationUser.PasswordHash)
                {
                    existingProfile.UpdatePasswordHash(
                        applicationUser.PasswordHash,
                        applicationUser.Id);
                    await context.SaveChangesAsync(cancellationToken);
                }

                return;
            }

            var passwordHash = applicationUser.PasswordHash
                ?? passwordHasher.HashPassword(applicationUser, password);

            var userProfile = new UserAggregate(
                applicationUser.Id,
                firstName: firstName,
                lastName: lastName,
                phoneNumber: phoneNumber,
                passwordHash: passwordHash,
                gender: gender,
                role: domainRole,
                dateOfBirth: new DateOnly(2000, 1, 1));

            await context.UsersProfiles.AddAsync(userProfile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static bool IsRecognizedLegacySeedProfile(
            UserAggregate? profile,
            string phoneNumber,
            string expectedFirstName,
            string expectedLastName)
        {
            if (profile is null ||
                !string.Equals(profile.PhoneNumber, phoneNumber, StringComparison.Ordinal))
            {
                return false;
            }

            if (phoneNumber == DemoSeedData.MainBuyerPhoneNumber)
            {
                return profile.Role == Role.User &&
                    string.Equals(profile.FirstName, "Quraaa", StringComparison.Ordinal) &&
                    string.Equals(profile.LastName, "User", StringComparison.Ordinal);
            }

            return IsLibraryOwnerPhoneNumber(phoneNumber) &&
                string.Equals(profile.FirstName, expectedFirstName, StringComparison.Ordinal) &&
                string.Equals(profile.LastName, expectedLastName, StringComparison.Ordinal) &&
                profile.Role is Role.User or Role.LibraryOwner;
        }

        private static bool HasCompatibleDomainRole(
            Role existingRole,
            Role expectedRole,
            string phoneNumber) =>
            existingRole == expectedRole ||
            (IsLibraryOwnerPhoneNumber(phoneNumber) &&
             existingRole == Role.LibraryOwner &&
             expectedRole == Role.User);

        private static bool IsLibraryOwnerPhoneNumber(string phoneNumber) =>
            LibraryOwnerPhoneNumbers.Contains(phoneNumber, StringComparer.Ordinal);

        private static async Task ApplyDemoCreationTimelineAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken)
        {
            var namedDaysAgo = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [DemoSeedData.SuperAdminPhoneNumber] = 720,
                [DemoSeedData.AdminPhoneNumber] = 540,
                [DemoSeedData.MainBuyerPhoneNumber] = 420,
                [DemoSeedData.SellerPhoneNumber] = 300,
                [DemoSeedData.CheckoutBuyerPhoneNumber] = 180,
                [DemoSeedData.ReporterOnePhoneNumber] = 90,
                [DemoSeedData.ReporterTwoPhoneNumber] = 45,
                [DemoSeedData.ReporterThreePhoneNumber] = 10,
            };
            var allPhoneNumbers = namedDaysAgo.Keys
                .Concat(LibraryOwnerPhoneNumbers)
                .ToArray();
            var profiles = await context.UsersProfiles
                .Where(profile => allPhoneNumbers.Contains(profile.PhoneNumber))
                .ToListAsync(cancellationToken);

            foreach (var profile in profiles)
            {
                var daysAgo = namedDaysAgo.TryGetValue(profile.PhoneNumber, out var namedAge)
                    ? namedAge
                    : 300 + Array.IndexOf(
                        LibraryOwnerPhoneNumbers.ToArray(),
                        profile.PhoneNumber) * 3;
                context.Entry(profile)
                    .Property(nameof(UserAggregate.CreationTime))
                    .CurrentValue = DateTime.UtcNow.AddDays(-daysAgo);
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureIdentityRoleAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ApplicationUser applicationUser,
            string role)
        {
            await EnsureRoleAsync(roleManager, role);

            if (await userManager.IsInRoleAsync(applicationUser, role))
            {
                return;
            }

            if (!string.Equals(role, Role.User.ToString(), StringComparison.Ordinal))
            {
                applicationUser.RefreshToken = null;
                applicationUser.RefreshTokenExpiryTime = DateTime.UtcNow;
                applicationUser.RefreshTokenFamilyId = null;
            }

            var roleResult = await userManager.AddToRoleAsync(applicationUser, role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign role {role} to {applicationUser.UserName}: {errors}");
            }
        }
    }
}
