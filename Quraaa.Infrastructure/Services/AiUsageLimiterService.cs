using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using StackExchange.Redis;

namespace Quraaa.Infrastructure.Services
{
    public class AiUsageLimiterService : IAiUsageLimiterService
    {
        private readonly IConnectionMultiplexer? _redis;
        private readonly IMemoryCache? _memoryCache;
        private readonly int _dailyLimit;

        // Reuses the IConnectionMultiplexer singleton already registered by
        // AddOtpCache — same Redis instance, different key prefix, no second
        // connection pool.
        public AiUsageLimiterService(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _dailyLimit = configuration.GetValue<int?>("AiAssistant:DailyFreeLimit") ?? 5;

            _redis = serviceProvider.GetService<IConnectionMultiplexer>();

            if (_redis == null)
            {
                _memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
            }
        }

        public async Task<AiUsageCheckResult> TryConsumeAsync(
            Guid userId, CancellationToken cancellationToken = default)
        {
            var key = $"ai-usage:{userId}:{DateTime.UtcNow:yyyy-MM-dd}";

            if (_redis != null)
            {
                var db = _redis.GetDatabase();
                // Atomic INCR — safe under concurrent requests from the same
                // user, unlike a get-then-set counter would be.
                var count = await db.StringIncrementAsync(key);

                if (count == 1)
                {
                    await db.KeyExpireAsync(key, TimeSpan.FromHours(25));// small buffer past the 24h day
                }

                return new AiUsageCheckResult(
                    Allowed: count <= _dailyLimit,
                    DailyLimit: _dailyLimit,
                    RequestsUsedToday: (int)count);
            }
            else
            {
                var currentCount = _memoryCache!.GetOrCreate(key, entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(25);
                    return 0;
                });

                var newCount = currentCount + 1;
                _memoryCache.Set(key, newCount);

                return new AiUsageCheckResult(
                    Allowed: newCount <= _dailyLimit,
                    DailyLimit: _dailyLimit,
                    RequestsUsedToday: newCount);
            }
        }
    }
}