using Quraaa.Application.Features.Authentication.Interfaces;
using Microsoft.EntityFrameworkCore;
using Quraaa.Domain.Shared.Exceptions;
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

        public async Task<UserAggregate> GetUserByIdAsync(Guid id)
        {
            var user = await _context.UsersProfiles
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

            return user ?? throw new NotFoundException("User was not found.");
        }

        public async Task<UserAggregate> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            var user = await _context.UsersProfiles
                .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted);

            return user ?? throw new NotFoundException("User was not found.");
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
