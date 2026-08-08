using Microsoft.Extensions.Caching.Distributed;
using Quraaa.Application.Features.Authentication.Interfaces;

namespace Quraaa.Infrastructure.Services
{
    public class AccessTokenRevocationService : IAccessTokenRevocationService
    {
        private const string CacheKeyPrefix = "auth:revoked-access-token:";
        private readonly IDistributedCache _cache;

        public AccessTokenRevocationService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task RevokeAsync(
            string tokenId,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                return;
            }

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt
            };

            await _cache.SetStringAsync(
                GetCacheKey(tokenId),
                "revoked",
                options,
                cancellationToken);
        }

        public async Task<bool> IsRevokedAsync(
            string tokenId,
            CancellationToken cancellationToken = default)
        {
            var value = await _cache.GetStringAsync(
                GetCacheKey(tokenId),
                cancellationToken);

            return value is not null;
        }

        private static string GetCacheKey(string tokenId) =>
            $"{CacheKeyPrefix}{tokenId}";
    }
}
