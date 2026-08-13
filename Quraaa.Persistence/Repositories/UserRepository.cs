using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Domain.User;
using Quraaa.Domain.Shared.Exceptions;
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

        public async Task AddUserAsync(
            UserAggregate user,
            CancellationToken cancellationToken = default)
        {
            await _context.UsersProfiles.AddAsync(user, cancellationToken);
        }

        public async Task<UserAggregate?> GetUserByIdAsync(Guid id)
        {
            return await _context.UsersProfiles
                .Include(u => u.Interests)
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        }

        public async Task<UserAggregate?> GetUserWithLocationsByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.UsersProfiles
                .Include(u => u.Locations)
                .FirstOrDefaultAsync(
                    u => u.Id == id && !u.IsDeleted,
                    cancellationToken);
        }

        public async Task<UserAggregate?> GetUserWithProfileDetailsByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _context.UsersProfiles
                .Include(u => u.Interests)
                .Include(u => u.Locations)
                .AsSplitQuery()
                .FirstOrDefaultAsync(
                    u => u.Id == id && !u.IsDeleted,
                    cancellationToken);
        }

        public async Task<UserAggregate?> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            return await _context.UsersProfiles
                        .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber && !u.IsDeleted);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "The profile changed in another request. Reload it and try again.");
            }
        }
    }
}
