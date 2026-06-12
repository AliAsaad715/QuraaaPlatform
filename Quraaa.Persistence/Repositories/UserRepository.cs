using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Authentication.Interfaces;
using Microsoft.EntityFrameworkCore;
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

        public async Task<UserAggregate?> GetUserByIdAsync(Guid id)
        {
            return await _context.UsersProfiles
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<UserAggregate?> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.UsersProfiles
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
