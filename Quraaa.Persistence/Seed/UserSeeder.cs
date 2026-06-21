using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            string role = Role.User.ToString();
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
            }

            await EnsureUserAsync(
                context,
                userManager,
                passwordHasher,
                DefaultUserPhoneNumber,
                firstName: "Quraaa",
                lastName: "User",
                gender: Gender.Male,
                role,
                cancellationToken);

            if (await context.Libraries.AnyAsync(cancellationToken))
            {
                return;
            }

            var ownerIndex = 1;
            foreach (var phoneNumber in LibraryOwnerPhoneNumbers)
            {
                await EnsureUserAsync(
                    context,
                    userManager,
                    passwordHasher,
                    phoneNumber,
                    firstName: "Library",
                    lastName: $"Owner {ownerIndex:000}",
                    gender: ownerIndex % 2 == 0 ? Gender.Female : Gender.Male,
                    role,
                    cancellationToken);

                ownerIndex++;
            }
        }

        private static string GetLibraryOwnerPhoneNumber(int index)
        {
            return $"+963930000{index:000}";
        }

        private static async Task EnsureUserAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IPasswordHasher<ApplicationUser> passwordHasher,
            string phoneNumber,
            string firstName,
            string lastName,
            Gender gender,
            string role,
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

            if (!await userManager.IsInRoleAsync(applicationUser, role))
            {
                var roleResult = await userManager.AddToRoleAsync(applicationUser, role);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to assign role {role} to {phoneNumber}: {errors}");
                }
            }

            if (await context.UsersProfiles.AnyAsync(u => u.Id == applicationUser.Id, cancellationToken))
            {
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
                role: Role.User,
                dateOfBirth: new DateOnly(2000, 1, 1));

            await context.UsersProfiles.AddAsync(userProfile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
