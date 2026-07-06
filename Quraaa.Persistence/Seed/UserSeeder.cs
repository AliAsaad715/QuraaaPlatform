using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quraaa.Domain.Library.Enums;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Seed
{
    public static class UserSeeder
    {
        public const int LibraryOwnerCount = 100;

        private const string DefaultUserPhoneNumber = "+963912345678";
        private const string SeedPassword = "User@12345";

        public static IReadOnlyList<string> LibraryOwnerPhoneNumbers { get; } =
            Enumerable.Range(1, LibraryOwnerCount)
                .Select(GetLibraryOwnerPhoneNumber)
                .ToArray();

        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            var userRole = Role.User.ToString();
            var libraryOwnerRole = Role.LibraryOwner.ToString();

            await EnsureRoleAsync(roleManager, userRole);
            await EnsureRoleAsync(roleManager, libraryOwnerRole);

            await EnsureUserAsync(
                context,
                userManager,
                roleManager,
                passwordHasher,
                DefaultUserPhoneNumber,
                firstName: "Quraaa",
                lastName: "User",
                gender: Gender.Male,
                domainRole: Role.User,
                identityRoles: new[] { userRole },
                cancellationToken);

            if (await context.Libraries.AnyAsync(cancellationToken))
            {
                await EnsureApprovedLibraryOwnerRolesAsync(
                    context,
                    userManager,
                    roleManager,
                    cancellationToken);
                return;
            }

            var ownerIndex = 1;
            foreach (var phoneNumber in LibraryOwnerPhoneNumbers)
            {
                await EnsureUserAsync(
                    context,
                    userManager,
                    roleManager,
                    passwordHasher,
                    phoneNumber,
                    firstName: "Library",
                    lastName: $"Owner {ownerIndex:000}",
                    gender: ownerIndex % 2 == 0 ? Gender.Female : Gender.Male,
                    domainRole: Role.User,
                    identityRoles: new[] { userRole },
                    cancellationToken);

                ownerIndex++;
            }
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

            var ownerUserIds = await context.Libraries
                .AsNoTracking()
                .Where(library => library.ApprovalStatus == LibraryApprovalStatus.Approved)
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
            string phoneNumber,
            string firstName,
            string lastName,
            Gender gender,
            Role domainRole,
            IReadOnlyCollection<string> identityRoles,
            CancellationToken cancellationToken)
        {
            var applicationUser = await userManager.FindByNameAsync(phoneNumber);
            if (applicationUser is null)
            {
                applicationUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = phoneNumber,
                    PhoneNumber = phoneNumber,
                    Email = $"{phoneNumber}@quraaa.com",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                };

                var identityResult = await userManager.CreateAsync(applicationUser, SeedPassword);
                if (!identityResult.Succeeded)
                {
                    var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to seed user account {phoneNumber}: {errors}");
                }
            }

            foreach (var identityRole in identityRoles.Distinct())
            {
                await EnsureIdentityRoleAsync(userManager, roleManager, applicationUser, identityRole);
            }

            var existingProfile = await context.UsersProfiles
                .FirstOrDefaultAsync(u => u.Id == applicationUser.Id, cancellationToken);

            if (existingProfile is not null)
            {
                if (existingProfile.Role != domainRole)
                {
                    if (domainRole == Role.LibraryOwner)
                    {
                        existingProfile.BecomeLibraryOwner(applicationUser.Id);
                    }
                    else
                    {
                        context.Entry(existingProfile).Property(nameof(UserAggregate.Role)).CurrentValue = domainRole;
                    }

                    await context.SaveChangesAsync(cancellationToken);
                }

                return;
            }

            var passwordHash = applicationUser.PasswordHash ?? passwordHasher.HashPassword(applicationUser, SeedPassword);

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

            var roleResult = await userManager.AddToRoleAsync(applicationUser, role);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign role {role} to {applicationUser.UserName}: {errors}");
            }
        }
    }
}
