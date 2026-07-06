using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Persistence.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Quraaa.Persistence.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _configuration;

        public IdentityService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public async Task<IdentityUserInfo?> GetUserIdentityByPhoneNumberAsync(string phoneNumber)
        {
            var user = await _userManager.FindByNameAsync(phoneNumber);
            return user is null
                ? null
                : new IdentityUserInfo(
                    user.Id,
                    user.PhoneNumber ?? phoneNumber,
                    user.PhoneNumberConfirmed);
        }

        public async Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber)
        {
            var user = await _userManager.FindByNameAsync(phoneNumber);
            return user == null;
        }

        public async Task<IdentityResultDto> CreateUserIdentityAsync(
            Guid id,
            string phoneNumber,
            string password,
            string role,
            bool phoneNumberConfirmed = true)
        {
            var identityUser = new ApplicationUser
            {
                Id = id,
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,
                Email = $"{phoneNumber}@quraaa.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = phoneNumberConfirmed
            };

            var result = await _userManager.CreateAsync(identityUser, password);
            if (!result.Succeeded)
            {
                var errorDescriptions = result.Errors.Select(e => e.Description);
                return IdentityResultDto.Failure(errorDescriptions);
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
            }
            await _userManager.AddToRoleAsync(identityUser, role);

            return IdentityResultDto.Success(identityUser.PasswordHash!);
        }

        public async Task<IdentityResultDto> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return IdentityResultDto.Failure(new[] { "User security identity was not found." });
            }

            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (!result.Succeeded)
            {
                var errorDescriptions = result.Errors.Select(e => e.Description);
                return IdentityResultDto.Failure(errorDescriptions);
            }

            return IdentityResultDto.Success(user.PasswordHash!);
        }

        public async Task<(bool Succeeded, string? UpdatedPasswordHash, IEnumerable<string> Errors)> ResetPasswordAsync(Guid userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return (false, null, new[] { "User not found" });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                return (false, null, result.Errors.Select(e => e.Description));
            }

            var refreshedUser = await _userManager.FindByIdAsync(userId.ToString());
            return (true, refreshedUser?.PasswordHash, Array.Empty<string>());
        }

        public async Task<IdentityResultDto> ConfirmPhoneNumberAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return IdentityResultDto.Failure(new[] { "User security identity was not found." });
            }

            user.PhoneNumberConfirmed = true;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return IdentityResultDto.Failure(result.Errors.Select(e => e.Description));
            }

            return IdentityResultDto.Success(user.PasswordHash ?? string.Empty);
        }

        public async Task<bool> CheckPasswordAsync(Guid userId, string password)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user is not null && await _userManager.CheckPasswordAsync(user, password);
        }

        public async Task<bool> IsInRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user is not null && await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<IdentityResultDto> AddUserToRoleAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return IdentityResultDto.Failure(new[] { "User security identity was not found." });
            }

            if (!await _roleManager.RoleExistsAsync(role))
            {
                var createRoleResult = await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
                if (!createRoleResult.Succeeded)
                {
                    return IdentityResultDto.Failure(createRoleResult.Errors.Select(e => e.Description));
                }
            }

            if (await _userManager.IsInRoleAsync(user, role))
            {
                return IdentityResultDto.Success(user.PasswordHash ?? string.Empty);
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, role);
            if (!addRoleResult.Succeeded)
            {
                return IdentityResultDto.Failure(addRoleResult.Errors.Select(e => e.Description));
            }

            return IdentityResultDto.Success(user.PasswordHash ?? string.Empty);
        }

        public async Task<AuthResponse> GenerateAuthTokensAsync(Guid userId, string phoneNumber)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new Exception("User security identity not found.");

            var secretKey = _configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT Secret Key is missing.");
            var issuer = _configuration["JWT_ISSUER"];
            var audience = _configuration["JWT_AUDIENCE"];
            var durationInMinutes = double.Parse(_configuration["JWT_DURATION_IN_MINUTES"] ?? "60");
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                expires: DateTime.UtcNow.AddMinutes(durationInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            string refreshToken = GenerateSecureRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(30);

            await _userManager.UpdateAsync(user);

            return new AuthResponse(
                accessToken,
                refreshToken,
                token.ValidTo
            );
        }

        public async Task<SignInResultDto> CheckPasswordAndGenerateTokensAsync(string phoneNumber, string password)
        {
            var user = await _userManager.FindByNameAsync(phoneNumber);
            if (user == null)
            {
                return SignInResultDto.Failure(SignInFailureReason.InvalidCredentials);
            }

            var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isPasswordValid)
            {
                return SignInResultDto.Failure(SignInFailureReason.InvalidCredentials);
            }

            if (!user.PhoneNumberConfirmed)
            {
                return SignInResultDto.Failure(SignInFailureReason.PhoneNumberNotConfirmed);
            }

            var authResponse = await GenerateAuthTokensAsync(user.Id, phoneNumber);
            return SignInResultDto.Success(authResponse);
        }

        private string GenerateSecureRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
