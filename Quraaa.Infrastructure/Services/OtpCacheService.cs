using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.Otp.Interfaces;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Text;

namespace Quraaa.Infrastructure.Services
{
    public class OtpCacheService : IOtpCacheService
    {
        private const string IncrementWithExpiryScript =
            "local current = redis.call('INCR', KEYS[1]) " +
            "redis.call('PEXPIRE', KEYS[1], ARGV[1]) " +
            "return current";

        private const string CompareAndDeleteScript =
            "if redis.call('GET', KEYS[1]) == ARGV[1] then " +
            "return redis.call('DEL', KEYS[1]) else return 0 end";

        private const string SetOwnedOtpScript =
            "if redis.call('GET', KEYS[1]) == ARGV[1] then " +
            "redis.call('PSETEX', KEYS[2], ARGV[2], ARGV[3]) " +
            "redis.call('PSETEX', KEYS[3], ARGV[2], ARGV[4]) " +
            "return 1 else return 0 end";

        private const string OwnedOtpValuePrefix = "v1:";
        private const string TaggedOtpMarkerValue = "1";
        private const int LockStripeCount = 256;

        private static readonly SemaphoreSlim[] CounterLocks = CreateLockStripes();
        private static readonly SemaphoreSlim[] CacheEntryLocks = CreateLockStripes();

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

        private static string GetTaggedOtpMarkerKey(string phoneNumber, string keyPrefix) =>
            $"{keyPrefix}:otp_tagged:{phoneNumber}";

        private static string GetLockoutKey(string phoneNumber, string keyPrefix) =>
            $"{keyPrefix}:otp_lockout:{phoneNumber}";

        private static string GetFailedVerificationAttemptKey(string targetKey, string keyPrefix) =>
            $"{keyPrefix}:otp_verify_failed:{targetKey}";

        private static string GetVerificationLockoutKey(string targetKey, string keyPrefix) =>
            $"{keyPrefix}:otp_verify_lockout:{targetKey}";

        public async Task SetOtpAsync(string phoneNumber, string otpCode, TimeSpan expiration, string keyPrefix, CancellationToken cancellationToken = default)
        {
            if (_redis is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _redis.GetDatabase().StringSetAsync(
                    GetRedisKey(GetOtpKey(phoneNumber, keyPrefix)),
                    otpCode,
                    expiration);
                return;
            }

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(GetOtpKey(phoneNumber, keyPrefix), otpCode, options, cancellationToken);
        }

        public async Task<string?> GetOtpAsync(string phoneNumber, string keyPrefix, CancellationToken cancellationToken = default)
        {
            var otpKey = GetOtpKey(phoneNumber, keyPrefix);
            var taggedOtpMarkerKey = GetTaggedOtpMarkerKey(phoneNumber, keyPrefix);
            string? cachedValue;

            if (_redis is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cachedValue = await GetRedisOtpValueAsync(
                    GetOtpRedisKey(otpKey, phoneNumber, keyPrefix),
                    distributedCacheKey: null,
                    cancellationToken);

                if (string.IsNullOrEmpty(cachedValue))
                {
                    var hasTaggedOtpMarker = await _redis.GetDatabase().KeyExistsAsync(
                        GetOtpRedisKey(taggedOtpMarkerKey, phoneNumber, keyPrefix));

                    if (!hasTaggedOtpMarker)
                    {
                        // Accept untagged string and RedisCache hash entries during
                        // their short remaining lifetime after deployment. Once a
                        // tagged OTP has existed, its marker prevents a failed resend
                        // cleanup from exposing the previous OTP again.
                        cachedValue = await GetRedisOtpValueAsync(
                            GetRedisKey(otpKey),
                            otpKey,
                            cancellationToken);
                    }
                }
            }
            else
            {
                cachedValue = await _cache.GetStringAsync(otpKey, cancellationToken);
            }

            return DecodeOtpValue(cachedValue);
        }

        public async Task ClearOtpAsync(string phoneNumber, string keyPrefix, CancellationToken cancellationToken = default)
        {
            var otpKey = GetOtpKey(phoneNumber, keyPrefix);
            var taggedOtpMarkerKey = GetTaggedOtpMarkerKey(phoneNumber, keyPrefix);
            if (_redis is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var database = _redis.GetDatabase();
                await database.KeyDeleteAsync(GetRedisKey(otpKey));
                await database.KeyDeleteAsync(
                    new RedisKey[]
                    {
                        GetOtpRedisKey(otpKey, phoneNumber, keyPrefix),
                        GetOtpRedisKey(taggedOtpMarkerKey, phoneNumber, keyPrefix)
                    });
                return;
            }

            await _cache.RemoveAsync(otpKey, cancellationToken);
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

        public async Task<bool> TryAcquireOtpRequestAsync(
            string targetKey,
            string ownerToken,
            TimeSpan lockoutPeriod,
            string keyPrefix,
            CancellationToken cancellationToken = default)
        {
            var lockoutKey = GetLockoutKey(targetKey, keyPrefix);

            if (_redis is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var database = _redis.GetDatabase();

                // Honor a residual lease after prior-version instances have
                // been drained. This supports a coordinated cutover; active
                // old instances cannot observe the tagged lease below.
                if (await database.KeyExistsAsync(GetRedisKey(lockoutKey)))
                {
                    return false;
                }

                return await database.StringSetAsync(
                    GetOtpRedisKey(lockoutKey, targetKey, keyPrefix),
                    ownerToken,
                    lockoutPeriod,
                    When.NotExists);
            }

            var entryLock = GetLockStripe(CacheEntryLocks, lockoutKey);
            await entryLock.WaitAsync(cancellationToken);
            try
            {
                var currentOwner = await _cache.GetStringAsync(lockoutKey, cancellationToken);
                if (!string.IsNullOrEmpty(currentOwner))
                {
                    return false;
                }

                await _cache.SetStringAsync(
                    lockoutKey,
                    ownerToken,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = lockoutPeriod
                    },
                    cancellationToken);

                return true;
            }
            finally
            {
                entryLock.Release();
            }
        }

        public async Task<bool> TrySetOwnedOtpAsync(
            string phoneNumber,
            string otpCode,
            string ownerToken,
            TimeSpan expiration,
            string keyPrefix,
            CancellationToken cancellationToken = default)
        {
            var lockoutKey = GetLockoutKey(phoneNumber, keyPrefix);
            var otpKey = GetOtpKey(phoneNumber, keyPrefix);
            var taggedOtpMarkerKey = GetTaggedOtpMarkerKey(phoneNumber, keyPrefix);
            var ownedOtpValue = EncodeOwnedOtpValue(ownerToken, otpCode);

            if (_redis is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var expirationMilliseconds = Math.Max(
                    1,
                    (long)Math.Ceiling(expiration.TotalMilliseconds));

                var result = await _redis.GetDatabase().ScriptEvaluateAsync(
                    SetOwnedOtpScript,
                    new RedisKey[]
                    {
                        GetOtpRedisKey(lockoutKey, phoneNumber, keyPrefix),
                        GetOtpRedisKey(otpKey, phoneNumber, keyPrefix),
                        GetOtpRedisKey(taggedOtpMarkerKey, phoneNumber, keyPrefix)
                    },
                    new RedisValue[]
                    {
                        ownerToken,
                        expirationMilliseconds,
                        ownedOtpValue,
                        TaggedOtpMarkerValue
                    });

                return (long)result == 1;
            }

            var entryLock = GetLockStripe(CacheEntryLocks, lockoutKey);
            await entryLock.WaitAsync(cancellationToken);
            try
            {
                var currentOwner = await _cache.GetStringAsync(lockoutKey, cancellationToken);
                if (!string.Equals(currentOwner, ownerToken, StringComparison.Ordinal))
                {
                    return false;
                }

                await _cache.SetStringAsync(
                    otpKey,
                    ownedOtpValue,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiration
                    },
                    cancellationToken);

                return true;
            }
            finally
            {
                entryLock.Release();
            }
        }

        public async Task ClearOwnedOtpAsync(
            string phoneNumber,
            string otpCode,
            string ownerToken,
            string keyPrefix,
            CancellationToken cancellationToken = default)
        {
            var otpKey = GetOtpKey(phoneNumber, keyPrefix);
            await CompareAndDeleteAsync(
                otpKey,
                EncodeOwnedOtpValue(ownerToken, otpCode),
                cancellationToken,
                synchronizationKey: GetLockoutKey(phoneNumber, keyPrefix),
                redisKey: GetOtpRedisKey(otpKey, phoneNumber, keyPrefix));
        }

        public async Task ReleaseOtpRequestAsync(
            string targetKey,
            string ownerToken,
            string keyPrefix,
            CancellationToken cancellationToken = default)
        {
            var lockoutKey = GetLockoutKey(targetKey, keyPrefix);
            await CompareAndDeleteAsync(
                lockoutKey,
                ownerToken,
                cancellationToken,
                redisKey: GetOtpRedisKey(lockoutKey, targetKey, keyPrefix));
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

            var counterLock = GetLockStripe(CounterLocks, key);
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
            var redisKey = GetRedisKey(key);
            var expirationMilliseconds = Math.Max(1, (long)Math.Ceiling(expiration.TotalMilliseconds));

            var result = await database.ScriptEvaluateAsync(
                IncrementWithExpiryScript,
                new RedisKey[] { redisKey },
                new RedisValue[] { expirationMilliseconds });

            return checked((int)(long)result);
        }

        private async Task CompareAndDeleteAsync(
            string key,
            string expectedValue,
            CancellationToken cancellationToken,
            string? synchronizationKey = null,
            RedisKey? redisKey = null)
        {
            if (_redis is not null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _redis.GetDatabase().ScriptEvaluateAsync(
                    CompareAndDeleteScript,
                    new RedisKey[] { redisKey ?? GetRedisKey(key) },
                    new RedisValue[] { expectedValue });
                return;
            }

            var entryLock = GetLockStripe(
                CacheEntryLocks,
                synchronizationKey ?? key);
            await entryLock.WaitAsync(cancellationToken);
            try
            {
                var currentValue = await _cache.GetStringAsync(key, cancellationToken);
                if (string.Equals(currentValue, expectedValue, StringComparison.Ordinal))
                {
                    await _cache.RemoveAsync(key, cancellationToken);
                }
            }
            finally
            {
                entryLock.Release();
            }
        }

        private RedisKey GetRedisKey(string key) => $"{_redisInstanceName}{key}";

        private RedisKey GetOtpRedisKey(
            string key,
            string targetKey,
            string keyPrefix)
        {
            var partitionSource = Encoding.UTF8.GetBytes(
                $"{_redisInstanceName}\n{keyPrefix}\n{targetKey}");
            var hashTag = Convert.ToHexString(SHA256.HashData(partitionSource));

            // Put the controlled tag first so braces in a configured instance
            // name cannot override the slot selected for this OTP partition.
            return $"{{{hashTag}}}:{_redisInstanceName}{key}";
        }

        private async Task<string?> GetRedisOtpValueAsync(
            RedisKey redisKey,
            string? distributedCacheKey,
            CancellationToken cancellationToken)
        {
            try
            {
                return await _redis!.GetDatabase().StringGetAsync(redisKey);
            }
            catch (RedisServerException exception)
                when (exception.Message.Contains("WRONGTYPE", StringComparison.Ordinal))
            {
                return distributedCacheKey is null
                    ? null
                    : await _cache.GetStringAsync(
                        distributedCacheKey,
                        cancellationToken);
            }
        }

        private static SemaphoreSlim[] CreateLockStripes()
        {
            return Enumerable.Range(0, LockStripeCount)
                .Select(_ => new SemaphoreSlim(1, 1))
                .ToArray();
        }

        private static SemaphoreSlim GetLockStripe(
            SemaphoreSlim[] lockStripes,
            string key)
        {
            var hash = unchecked((uint)StringComparer.Ordinal.GetHashCode(key));
            return lockStripes[hash % (uint)lockStripes.Length];
        }

        private static string EncodeOwnedOtpValue(string ownerToken, string otpCode) =>
            $"{OwnedOtpValuePrefix}{ownerToken}:{otpCode}";

        private static string? DecodeOtpValue(string? cachedValue)
        {
            if (string.IsNullOrEmpty(cachedValue)
                || !cachedValue.StartsWith(OwnedOtpValuePrefix, StringComparison.Ordinal))
            {
                return cachedValue;
            }

            var codeSeparatorIndex = cachedValue.LastIndexOf(':');
            return codeSeparatorIndex < OwnedOtpValuePrefix.Length
                ? null
                : cachedValue[(codeSeparatorIndex + 1)..];
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
