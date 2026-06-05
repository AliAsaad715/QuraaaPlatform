namespace Quraaa.Application.Features.Authentication.Interfaces
{
    public interface IIdentityService
    {
        Task<bool> IsPhoneNumberUniqueAsync(string phoneNumber);
        Task<string> CreateUserIdentityAsync(Guid id, string phoneNumber, string password);
    }
}
