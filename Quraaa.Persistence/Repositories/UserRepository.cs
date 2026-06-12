using Microsoft.EntityFrameworkCore;
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

        public async Task<UserAggregate?> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.UsersProfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
