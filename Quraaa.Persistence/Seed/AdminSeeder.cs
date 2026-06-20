using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Quraaa.Domain.User;
using Quraaa.Domain.User.Enums;
using Quraaa.Persistence.Data;
using System.Data;

namespace Quraaa.Persistence.Seed
{
    public static class AdminSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IConfiguration configuration,
            CancellationToken cancellationToken = default)
        {
            var phoneNumber = configuration["ADMIN_PHONE_NUMBER"];
            var password = configuration["ADMIN_PASSWORD"];

            if (string.IsNullOrWhiteSpace(phoneNumber) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            if (await userManager.FindByNameAsync(phoneNumber) is not null)
            {
                return;
            }

            var id = Guid.NewGuid();

            var applicationUser = new ApplicationUser
            {
                Id = id,
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,
                Email = $"{phoneNumber}@quraaa.com",
                EmailConfirmed = true,
            };

            var identityResult = await userManager.CreateAsync(applicationUser, password);
            if (!identityResult.Succeeded)
            {
                var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed admin account: {errors}");
            }

            string role = Role.Admin.ToString();
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
            }
            await userManager.AddToRoleAsync(applicationUser, role);

            var passwordHash = passwordHasher.HashPassword(applicationUser, password);

            var adminProfile = new UserAggregate(
                id,
                firstName: "Quraaa",
                lastName: "Admin",
                phoneNumber: "+963987654321",
                passwordHash: passwordHash,
                gender: Gender.Male,
                role: Role.Admin,
                dateOfBirth: new DateOnly(2000, 1, 1));

            await context.UsersProfiles.AddAsync(adminProfile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}