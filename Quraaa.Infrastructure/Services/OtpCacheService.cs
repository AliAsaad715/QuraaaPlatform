using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.Otp.Interfaces;
using StackExchange.Redis;
using System.Collections.Concurrent;

namespace Quraaa.Infrastructure.Services
{
    public class OtpCacheService : IOtpCacheService
    {
        private const string IncrementWithExpiryScript =
            "local current = redis.call('INCR', KEYS[1]) " +
            "redis.call('PEXPIRE', KEYS[1], ARGV[1]) " +
            "return current";

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> CounterLocks = new();

        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer? _redis;
        private readonly string _redisInstanceName;

        public OtpCacheService(
            IDistributedCache cache,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _cache = cache;
            _redis = serviceProvider.GetService<IConnectionMultiplexer>();
            _redisInstanceName = configuration["Redis:InstanceName"] ?? "Quraaa:Otp:";
        }

        private static string GetOtpKey(string phoneNumber, string keyPrefix) =>
            $"{keyPrefix}:otp:{phoneNumber}";

        private static string GetLockoutKey(string phoneNumber, string keyPrefix) =>
            $"{keyPrefix}:otp_lockout:{phoneNumber}";

        private static string GetFailedVerificationAttemptKey(string targetKey, string keyPrefix) =>
            $"{keyPrefix}:otp_verify_failed:{targetKey}";

        private static string GetVerificationLockoutKey(string targetKey, string keyPrefix) =>
            $"{keyPrefix}:otp_verify_lockout:{targetKey}";

        public async Task SetOtpAsync(string phoneNumber, string otpCode, TimeSpan expiration, string keyPrefix, CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(GetOtpKey(phoneNumber, keyPrefix), otpCode, options, cancellationToken);
        }

        public async Task<string?> GetOtpAsync(string phoneNumber, string keyPrefix, CancellationToken cancellationToken = default)
        {
            return await _cache.GetStringAsync(GetOtpKey(phoneNumber, keyPrefix), cancellationToken);
        }

        public async Task ClearOtpAsync(string phoneNumber, string keyPrefix, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetOtpKey(phoneNumber, keyPrefix), cancellationToken);
        }

        public async Task<bool> HasRecentOtpRequestAsync(string phoneNumber, TimeSpan lockoutPeriod, string keyPrefix, CancellationToken cancellationToken = default)
        {
            var lockout = await _cache.GetStringAsync(GetLockoutKey(phoneNumber, keyPrefix), cancellationToken);
            return !string.IsNullOrEmpty(lockout);
        }

        public async Task RecordOtpRequestAsync(string phoneNumber, TimeSpan lockoutPeriod, string keyPrefix, CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lockoutPeriod
            };

            await _cache.SetStringAsync(GetLockoutKey(phoneNumber, keyPrefix), "1", options, cancellationToken);
        }

        public async Task ClearOtpRequestAsync(string phoneNumber, string keyPrefix, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetLockoutKey(phoneNumber, keyPrefix), cancellationToken);
        }

        public async Task<int> IncrementFailedVerificationAttemptAsync(
            string targetKey,
            TimeSpan expiration,
            string keyPrefix,
            CancellationToken cancellationToken = default)
        {
            var key = GetFailedVerificationAttemptKey(targetKey, keyPrefix);

            if (_redis is not null)
            {
                return await IncrementRedisCounterAsync(key, expiration);
            }

            var counterLock = CounterLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await counterLock.WaitAsync(cancellationToken);
            try
            {
                return await IncrementDistributedCacheCounterAsync(key, expiration, cancellationToken);
            }
            finally
            {
                counterLock.Release();
            }
        }

        private async Task<int> IncrementDistributedCacheCounterAsync(
            string key,
            TimeSpan expiration,
            CancellationToken cancellationToken)
        {
            var currentValue = await _cache.GetStringAsync(key, cancellationToken);
            var currentCount = int.TryParse(currentValue, out var parsedCount) ? parsedCount : 0;
            var nextCount = currentCount + 1;

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(key, nextCount.ToString(), options, cancellationToken);

            return nextCount;
        }

        private async Task<int> IncrementRedisCounterAsync(string key, TimeSpan expiration)
        {
            var database = _redis!.GetDatabase();
            var redisKey = $"{_redisInstanceName}{key}";
            var expirationMilliseconds = Math.Max(1, (long)Math.Ceiling(expiration.TotalMilliseconds));

            var result = await database.ScriptEvaluateAsync(
                IncrementWithExpiryScript,
                new RedisKey[] { redisKey },
                new RedisValue[] { expirationMilliseconds });

            return checked((int)(long)result);
        }

        public async Task ClearFailedVerificationAttemptsAsync(string targetKey, string keyPrefix, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetFailedVerificationAttemptKey(targetKey, keyPrefix), cancellationToken);
        }

        public async Task<bool> IsVerificationLockedOutAsync(string targetKey, string keyPrefix, CancellationToken cancellationToken = default)
        {
            var lockout = await _cache.GetStringAsync(GetVerificationLockoutKey(targetKey, keyPrefix), cancellationToken);
            return !string.IsNullOrEmpty(lockout);
        }

        public async Task RecordVerificationLockoutAsync(
            string targetKey,
            TimeSpan lockoutPeriod,
            string keyPrefix,
            CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lockoutPeriod
            };

            await _cache.SetStringAsync(GetVerificationLockoutKey(targetKey, keyPrefix), "1", options, cancellationToken);
        }

        public async Task ClearVerificationLockoutAsync(string targetKey, string keyPrefix, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetVerificationLockoutKey(targetKey, keyPrefix), cancellationToken);
        }
    }
}
