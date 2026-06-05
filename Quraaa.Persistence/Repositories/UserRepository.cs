using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Domain.User;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddUserAsync(UserAggregate user)
        {
            await _context.UsersProfiles.AddAsync(user);
        }

        public Task<UserAggregate> GetUserByIdAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<UserAggregate> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            throw new NotImplementedException();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
