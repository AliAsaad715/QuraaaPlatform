using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        private const string RefreshTokenHashPrefix = "sha256:";
        private const int RefreshTokenSizeInBytes = 64;

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration,
            ApplicationDbContext dbContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _dbContext = dbContext;
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

            // UserManager persists the password hash, security stamp, and these
            // refresh-token fields in one concurrency-checked user update.
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow;
            user.RefreshTokenFamilyId = null;

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

            // Keep password recovery and refresh-token revocation atomic at the
            // Identity-user row. A successful reset cannot leave the old token live.
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow;
            user.RefreshTokenFamilyId = null;

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

        public async Task RevokeRefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidRawRefreshToken(refreshToken))
            {
                return;
            }

            var refreshTokenHash = HashRefreshToken(refreshToken);
            var revokedAt = DateTime.UtcNow;
            var concurrencyStamp = Guid.NewGuid().ToString();

            // Try the indexed current-token path first. If refresh rotation wins
            // the race, this update affects zero rows and the consumed-token lookup
            // below resolves the stable family and revokes its replacement.
            var affectedUsers = await _userManager.Users
                .Where(user => user.RefreshToken == refreshTokenHash
                    || user.RefreshToken == refreshToken)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(user => user.RefreshToken, (string?)null)
                    .SetProperty(user => user.RefreshTokenExpiryTime, revokedAt)
                    .SetProperty(user => user.RefreshTokenFamilyId, (Guid?)null)
                    .SetProperty(user => user.ConcurrencyStamp, concurrencyStamp),
                    cancellationToken);

            if (affectedUsers > 0)
            {
                return;
            }

            await RevokeFamilyForConsumedTokenAsync(
                refreshTokenHash,
                revokedAt,
                cancellationToken);
        }

        public async Task<AuthResponse> GenerateAuthTokensAsync(Guid userId, string phoneNumber)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new InvalidOperationException("User security identity not found.");
            }

            return await GenerateAndPersistAuthTokensAsync(
                user,
                phoneNumber,
                Guid.NewGuid(),
                presentedRefreshToken: null,
                presentedRefreshTokenExpiresAtUtc: null,
                cancellationToken: default);
        }

        public async Task<AuthResponse?> RefreshAuthTokensAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidRawRefreshToken(refreshToken))
            {
                return null;
            }

            var refreshTokenHash = HashRefreshToken(refreshToken);
            var user = await _userManager.Users.SingleOrDefaultAsync(candidate =>
                candidate.RefreshToken == refreshTokenHash
                || candidate.RefreshToken == refreshToken,
                cancellationToken);

            var now = DateTime.UtcNow;

            if (user is null)
            {
                // A consumed, still-live predecessor is a replay. Revoking its
                // active family makes any descendant refresh/access token unusable.
                await RevokeFamilyForConsumedTokenAsync(
                    refreshTokenHash,
                    now,
                    cancellationToken);
                return null;
            }

            if (!user.PhoneNumberConfirmed
                || user.RefreshTokenExpiryTime <= now)
            {
                await RevokeRefreshTokenAsync(refreshToken, cancellationToken);
                return null;
            }

            var familyId = user.RefreshTokenFamilyId ?? Guid.NewGuid();

            try
            {
                return await GenerateAndPersistAuthTokensAsync(
                    user,
                    user.PhoneNumber ?? string.Empty,
                    familyId,
                    refreshToken,
                    user.RefreshTokenExpiryTime,
                    cancellationToken);
            }
            catch (AuthTokenPersistenceException exception)
                when (exception.IsConcurrencyFailure)
            {
                // The winner archives this token before committing. Resolve that
                // record after the concurrency loss and revoke the winning family.
                await RevokeFamilyForConsumedTokenAsync(
                    refreshTokenHash,
                    now,
                    cancellationToken);
                return null;
            }
        }

        public async Task<bool> IsRefreshTokenFamilyActiveAsync(
            Guid userId,
            Guid familyId,
            CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty || familyId == Guid.Empty)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            return await _userManager.Users.AnyAsync(user =>
                user.Id == userId
                && user.RefreshTokenFamilyId == familyId
                && user.RefreshToken != null
                && user.RefreshTokenExpiryTime > now,
                cancellationToken);
        }

        private async Task<AuthResponse> GenerateAndPersistAuthTokensAsync(
            ApplicationUser user,
            string phoneNumber,
            Guid familyId,
            string? presentedRefreshToken,
            DateTime? presentedRefreshTokenExpiresAtUtc,
            CancellationToken cancellationToken)
        {
            var secretKey = _configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT Secret Key is missing.");
            var issuer = _configuration["JWT_ISSUER"];
            var audience = _configuration["JWT_AUDIENCE"];
            var durationInMinutes = double.Parse(_configuration["JWT_DURATION_IN_MINUTES"] ?? "60");
            var userRoles = await _userManager.GetRolesAsync(user);
            var issuedAtUtc = DateTime.UtcNow;

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.MobilePhone, user.PhoneNumber ?? phoneNumber),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(AuthenticationClaimNames.SessionId, familyId.ToString())
            };

            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                expires: issuedAtUtc.AddMinutes(durationInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateSecureRefreshToken();
            var refreshTokenHash = HashRefreshToken(refreshToken);
            var refreshTokenExpiresAtUtc = issuedAtUtc.AddDays(30);
            var startsNewFamily = presentedRefreshToken is null;

            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken);

            var userUpdate = _userManager.Users.Where(candidate =>
                candidate.Id == user.Id
                && candidate.ConcurrencyStamp == user.ConcurrencyStamp);

            if (!startsNewFamily)
            {
                var presentedRefreshTokenHash = HashRefreshToken(presentedRefreshToken!);
                var expectedFamilyId = user.RefreshTokenFamilyId;

                userUpdate = userUpdate.Where(candidate =>
                    candidate.RefreshTokenFamilyId == expectedFamilyId
                    && (candidate.RefreshToken == presentedRefreshTokenHash
                        || candidate.RefreshToken == presentedRefreshToken));
            }

            var affectedUsers = await userUpdate.ExecuteUpdateAsync(setters => setters
                .SetProperty(candidate => candidate.RefreshToken, refreshTokenHash)
                .SetProperty(candidate => candidate.RefreshTokenExpiryTime, refreshTokenExpiresAtUtc)
                .SetProperty(candidate => candidate.RefreshTokenFamilyId, familyId)
                .SetProperty(candidate => candidate.ConcurrencyStamp, Guid.NewGuid().ToString()),
                cancellationToken);

            if (affectedUsers != 1)
            {
                throw new AuthTokenPersistenceException(
                    new[] { "The refresh-token family changed concurrently." },
                    isConcurrencyFailure: true);
            }

            if (startsNewFamily)
            {
                // A fresh login is a new session family. Old-family records must
                // not be able to revoke this independently authenticated session.
                await _dbContext.ConsumedRefreshTokens
                    .Where(consumed => consumed.UserId == user.Id)
                    .ExecuteDeleteAsync(cancellationToken);
            }
            else
            {
                await _dbContext.ConsumedRefreshTokens
                    .Where(consumed => consumed.UserId == user.Id
                        && consumed.ExpiresAtUtc <= issuedAtUtc)
                    .ExecuteDeleteAsync(cancellationToken);

                _dbContext.ConsumedRefreshTokens.Add(new ConsumedRefreshToken(
                    user.Id,
                    familyId,
                    HashRefreshToken(presentedRefreshToken!),
                    issuedAtUtc,
                    presentedRefreshTokenExpiresAtUtc!.Value));

                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return new AuthResponse(
                accessToken,
                refreshToken,
                token.ValidTo
            );
        }

        private async Task RevokeFamilyForConsumedTokenAsync(
            string refreshTokenHash,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken)
        {
            var consumedToken = await _dbContext.ConsumedRefreshTokens
                .AsNoTracking()
                .SingleOrDefaultAsync(candidate =>
                    candidate.TokenHash == refreshTokenHash
                    && candidate.ExpiresAtUtc > revokedAtUtc,
                    cancellationToken);

            if (consumedToken is null)
            {
                return;
            }

            await RevokeRefreshTokenFamilyAsync(
                consumedToken.UserId,
                consumedToken.FamilyId,
                revokedAtUtc,
                cancellationToken);
        }

        private async Task RevokeRefreshTokenFamilyAsync(
            Guid userId,
            Guid familyId,
            DateTime revokedAtUtc,
            CancellationToken cancellationToken)
        {
            await _userManager.Users
                .Where(user => user.Id == userId
                    && user.RefreshTokenFamilyId == familyId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(user => user.RefreshToken, (string?)null)
                    .SetProperty(user => user.RefreshTokenExpiryTime, revokedAtUtc)
                    .SetProperty(user => user.RefreshTokenFamilyId, (Guid?)null)
                    .SetProperty(user => user.ConcurrencyStamp, Guid.NewGuid().ToString()),
                    cancellationToken);
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

        private static string GenerateSecureRefreshToken()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(RefreshTokenSizeInBytes));
        }

        private static bool IsValidRawRefreshToken(string? refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)
                || refreshToken.StartsWith(
                    RefreshTokenHashPrefix,
                    StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                return Convert.FromBase64String(refreshToken).Length
                    == RefreshTokenSizeInBytes;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string HashRefreshToken(string refreshToken)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return $"{RefreshTokenHashPrefix}{Convert.ToBase64String(hash)}";
        }

        private sealed class AuthTokenPersistenceException : Exception
        {
            public bool IsConcurrencyFailure { get; }

            public AuthTokenPersistenceException(
                IEnumerable<string> errors,
                bool isConcurrencyFailure)
                : base($"Authentication tokens could not be persisted: {string.Join(" | ", errors)}")
            {
                IsConcurrencyFailure = isConcurrencyFailure;
            }
        }
    }
}
