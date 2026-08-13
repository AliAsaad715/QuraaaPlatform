using Quraaa.Domain.User;

namespace Quraaa.Application.Features.Authentication.Interfaces
{
    public interface IUserRepository
    {
        Task<UserAggregate?> GetUserByIdAsync(Guid id);
        Task<UserAggregate?> GetUserWithLocationsByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);
        Task<UserAggregate?> GetUserWithProfileDetailsByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);
        Task<UserAggregate?> GetUserByPhoneNumberAsync(string phoneNumber);
        Task AddUserAsync(UserAggregate user, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
