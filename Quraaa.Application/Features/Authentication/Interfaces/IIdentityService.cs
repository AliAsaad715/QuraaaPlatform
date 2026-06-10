using Quraaa.Application.Features.Authentication.Common;

namespace Quraaa.Application.Features.Authentication.Interfaces
{
    public interface IIdentityService
    {
        Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber);
        Task<IdentityResultDto> CreateUserIdentityAsync(Guid id, string phoneNumber, string password);
        Task<IdentityResultDto> ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
        Task<AuthResponse> GenerateAuthTokensAsync(Guid userId, string phoneNumber);
    }
}
