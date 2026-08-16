using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quraaa.Application.Features.Authentication.Common;
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

            if (password.Length < AuthenticationPasswordPolicy.MinimumLength
                || password.Length > AuthenticationPasswordPolicy.MaximumLength)
            {
                throw new InvalidOperationException(
                    $"ADMIN_PASSWORD must contain {AuthenticationPasswordPolicy.MinimumLength} through {AuthenticationPasswordPolicy.MaximumLength} characters.");
            }

            // Keep the existing configuration keys for deployment compatibility,
            // but the bootstrap account has the single privileged platform role.
            string superAdminRole = Role.SuperAdmin.ToString();

            if (!await roleManager.RoleExistsAsync(superAdminRole))
            {
                var createRoleResult = await roleManager.CreateAsync(
                    new IdentityRole<Guid> { Name = superAdminRole });
                if (!createRoleResult.Succeeded)
                {
                    var errors = string.Join("; ", createRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create super admin role: {errors}");
                }
            }

            var existingUser = await userManager.FindByNameAsync(phoneNumber);
            if (existingUser is not null)
            {
                var identityChanged = false;
                if (!existingUser.PhoneNumberConfirmed)
                {
                    existingUser.PhoneNumberConfirmed = true;
                    identityChanged = true;
                }

                if (!await userManager.IsInRoleAsync(existingUser, superAdminRole))
                {
                    // A privileged role must not be inherited by a refresh-token
                    // family authenticated before the role elevation.
                    existingUser.RefreshToken = null;
                    existingUser.RefreshTokenExpiryTime = DateTime.UtcNow;
                    existingUser.RefreshTokenFamilyId = null;

                    var assignExistingRoleResult = await userManager.AddToRoleAsync(
                        existingUser,
                        superAdminRole);
                    if (!assignExistingRoleResult.Succeeded)
                    {
                        var errors = string.Join(
                            "; ",
                            assignExistingRoleResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to assign super admin role: {errors}");
                    }
                }

                if (identityChanged)
                {
                    var updateResult = await userManager.UpdateAsync(existingUser);
                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to update super admin account: {errors}");
                    }
                }

                if (!await userManager.CheckPasswordAsync(existingUser, password))
                {
                    var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);

                    // Persist the configured password change and refresh-token
                    // revocation in the same Identity user update.
                    existingUser.RefreshToken = null;
                    existingUser.RefreshTokenExpiryTime = DateTime.UtcNow;
                    existingUser.RefreshTokenFamilyId = null;

                    var resetResult = await userManager.ResetPasswordAsync(existingUser, resetToken, password);
                    if (!resetResult.Succeeded)
                    {
                        var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to reset seeded super admin password: {errors}");
                    }

                    existingUser = await userManager.FindByIdAsync(existingUser.Id.ToString()) ?? existingUser;
                }

                await EnsureSuperAdminProfileAsync(context, passwordHasher, existingUser, phoneNumber, cancellationToken);
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
                PhoneNumberConfirmed = true,
            };

            var identityResult = await userManager.CreateAsync(applicationUser, password);
            if (!identityResult.Succeeded)
            {
                var errors = string.Join("; ", identityResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed super admin account: {errors}");
            }

            var assignNewRoleResult = await userManager.AddToRoleAsync(
                applicationUser,
                superAdminRole);
            if (!assignNewRoleResult.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    assignNewRoleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign super admin role: {errors}");
            }

            var passwordHash = applicationUser.PasswordHash
                ?? passwordHasher.HashPassword(applicationUser, password);

            var superAdminProfile = new UserAggregate(
                id,
                firstName: "Quraaa",
                lastName: "Super Admin",
                phoneNumber: phoneNumber,
                passwordHash: passwordHash,
                gender: Gender.Male,
                role: Role.SuperAdmin,
                dateOfBirth: new DateOnly(2000, 1, 1));

            await context.UsersProfiles.AddAsync(superAdminProfile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }

        private static async Task EnsureSuperAdminProfileAsync(
            ApplicationDbContext context,
            IPasswordHasher<ApplicationUser> passwordHasher,
            ApplicationUser applicationUser,
            string phoneNumber,
            CancellationToken cancellationToken)
        {
            var superAdminProfile = await context.UsersProfiles
                .FirstOrDefaultAsync(user => user.Id == applicationUser.Id, cancellationToken);

            if (superAdminProfile is null)
            {
                var passwordHash = applicationUser.PasswordHash
                    ?? passwordHasher.HashPassword(applicationUser, string.Empty);

                superAdminProfile = new UserAggregate(
                    applicationUser.Id,
                    firstName: "Quraaa",
                    lastName: "Super Admin",
                    phoneNumber: phoneNumber,
                    passwordHash: passwordHash,
                    gender: Gender.Male,
                    role: Role.SuperAdmin,
                    dateOfBirth: new DateOnly(2000, 1, 1));

                await context.UsersProfiles.AddAsync(superAdminProfile, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                return;
            }

            var profileChanged = false;
            if (superAdminProfile.PhoneNumber != phoneNumber)
            {
                context.Entry(superAdminProfile).Property(nameof(UserAggregate.PhoneNumber)).CurrentValue = phoneNumber;
                profileChanged = true;
            }

            if (superAdminProfile.Role != Role.SuperAdmin)
            {
                superAdminProfile.BecomeSuperAdmin(applicationUser.Id);
                profileChanged = true;
            }

            if (!string.IsNullOrWhiteSpace(applicationUser.PasswordHash)
                && superAdminProfile.PasswordHash != applicationUser.PasswordHash)
            {
                superAdminProfile.UpdatePasswordHash(applicationUser.PasswordHash, applicationUser.Id);
                profileChanged = true;
            }

            if (profileChanged)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
