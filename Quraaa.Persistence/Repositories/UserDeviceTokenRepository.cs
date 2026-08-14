using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class UserDeviceTokenRepository : IUserDeviceTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public UserDeviceTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task UpsertAsync(
            Guid userId,
            string deviceToken,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.UserDeviceTokens
                .FirstOrDefaultAsync(t => t.DeviceToken == deviceToken, cancellationToken);

            if (existing is null)
            {
                await _context.UserDeviceTokens.AddAsync(
                    new UserDeviceToken(userId, deviceToken, nowUtc), cancellationToken);
                return;
            }

            if (existing.UserId != userId)
            {
                existing.Reassign(userId, nowUtc);
            }
            else
            {
                existing.Touch(nowUtc);
            }
        }

        public async Task<HashSet<string>> GetTokensByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            if (userIds.Count == 0)
            {
                return [];
            }

            var tokens = await _context.UserDeviceTokens
                .AsNoTracking()
                .Where(t => userIds.Contains(t.UserId))
                .Select(t => t.DeviceToken)
                .ToListAsync(cancellationToken);

            return tokens.ToHashSet(StringComparer.Ordinal);
        }

        public Task RemoveTokensAsync(
            IReadOnlyCollection<string> deviceTokens,
            CancellationToken cancellationToken = default)
        {
            if (deviceTokens.Count == 0)
            {
                return Task.CompletedTask;
            }

            return _context.UserDeviceTokens
                .Where(t => deviceTokens.Contains(t.DeviceToken))
                .ExecuteDeleteAsync(cancellationToken);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
    }
}
