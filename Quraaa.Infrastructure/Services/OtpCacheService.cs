using Microsoft.Extensions.Caching.Distributed;
using Quraaa.Application.Features.Otp.Interfaces;

namespace Quraaa.Infrastructure.Services
{
    public class OtpCacheService : IOtpCacheService
    {
        private readonly IDistributedCache _cache;

        public OtpCacheService(IDistributedCache cache)
        {
            _cache = cache;
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
