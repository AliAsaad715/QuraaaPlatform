using Quraaa.Domain.User;

namespace Quraaa.Application.Features.Authentication.Interfaces
{
    public interface IUserRepository
    {
        Task<UserAggregate?> GetUserByIdAsync(Guid id);
        Task<UserAggregate?> GetUserByPhoneNumberAsync(string phoneNumber);
        Task AddUserAsync(UserAggregate user);
        Task SaveChangesAsync();
    }
}
