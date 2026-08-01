using Quraaa.Application.Features.Authentication.Common;

namespace Quraaa.Application.Features.Authentication.Interfaces
{
    public interface IIdentityService
    {
        Task<IdentityUserInfo?> GetUserIdentityByPhoneNumberAsync(string phoneNumber);
        Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber);
        Task<IdentityResultDto> CreateUserIdentityAsync(Guid id, string phoneNumber, string password, string role, bool phoneNumberConfirmed = true);
        Task<IdentityResultDto> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
        Task<(bool Succeeded, string? UpdatedPasswordHash, IEnumerable<string> Errors)> ResetPasswordAsync(Guid userId, string newPassword);
        Task<IdentityResultDto> ConfirmPhoneNumberAsync(Guid userId);
        Task<bool> CheckPasswordAsync(Guid userId, string password);
        Task<bool> IsInRoleAsync(Guid userId, string role);
        Task<IdentityResultDto> AddUserToRoleAsync(Guid userId, string role);
        Task RevokeRefreshTokenAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);
        Task<AuthResponse?> RefreshAuthTokensAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);
        Task<bool> IsRefreshTokenFamilyActiveAsync(
            Guid userId,
            Guid familyId,
            CancellationToken cancellationToken = default);
        Task<AuthResponse> GenerateAuthTokensAsync(Guid userId, string phoneNumber);
        Task<SignInResultDto> CheckPasswordAndGenerateTokensAsync(string phoneNumber, string password);
    }
}
