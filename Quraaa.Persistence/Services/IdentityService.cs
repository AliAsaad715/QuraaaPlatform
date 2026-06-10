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
        private readonly IConfiguration _configuration;

        public IdentityService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber)
        {
            var user = await _userManager.FindByNameAsync(phoneNumber);
            return user == null;
        }

        public async Task<IdentityResultDto> CreateUserIdentityAsync(Guid id, string phoneNumber, string password)
        {
            var identityUser = new ApplicationUser
            {
                Id = id,
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,
                Email = $"{phoneNumber}@quraaa.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };

            var result = await _userManager.CreateAsync(identityUser, password);
            if (!result.Succeeded)
            {
                var errorDescriptions = result.Errors.Select(e => e.Description);
                return IdentityResultDto.Failure(errorDescriptions);
            }

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

        public async Task<AuthResponse> GenerateAuthTokensAsync(Guid userId, string phoneNumber)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) throw new Exception("User security identity not found.");

            var secretKey = _configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT Secret Key is missing.");
            var issuer = _configuration["JWT_ISSUER"];
            var audience = _configuration["JWT_AUDIENCE"];
            var durationInMinutes = double.Parse(_configuration["JWT_DURATION_IN_MINUTES"] ?? "60");

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

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
                user.Id,
                accessToken,
                refreshToken,
                token.ValidTo
            );
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
