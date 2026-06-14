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

        private static string GetOtpKey(string phoneNumber) => $"otp:{phoneNumber}";
        private static string GetLockoutKey(string phoneNumber) => $"otp_lockout:{phoneNumber}";
        private static string GetFailedVerificationAttemptKey(string targetKey) => $"otp_verify_failed:{targetKey}";
        private static string GetVerificationLockoutKey(string targetKey) => $"otp_verify_lockout:{targetKey}";

        public async Task SetOtpAsync(string phoneNumber, string otpCode, TimeSpan expiration, CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(GetOtpKey(phoneNumber), otpCode, options, cancellationToken);
        }

        public async Task<string?> GetOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            return await _cache.GetStringAsync(GetOtpKey(phoneNumber), cancellationToken);
        }

        public async Task ClearOtpAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetOtpKey(phoneNumber), cancellationToken);
        }

        public async Task<bool> HasRecentOtpRequestAsync(string phoneNumber, TimeSpan lockoutPeriod, CancellationToken cancellationToken = default)
        {
            var lockout = await _cache.GetStringAsync(GetLockoutKey(phoneNumber), cancellationToken);
            return !string.IsNullOrEmpty(lockout);
        }

        public async Task RecordOtpRequestAsync(string phoneNumber, TimeSpan lockoutPeriod, CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lockoutPeriod
            };

            await _cache.SetStringAsync(GetLockoutKey(phoneNumber), "1", options, cancellationToken);
        }

        public async Task ClearOtpRequestAsync(string phoneNumber, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetLockoutKey(phoneNumber), cancellationToken);
        }

        public async Task<int> IncrementFailedVerificationAttemptAsync(
            string targetKey,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
        {
            var key = GetFailedVerificationAttemptKey(targetKey);
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

        public async Task ClearFailedVerificationAttemptsAsync(string targetKey, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetFailedVerificationAttemptKey(targetKey), cancellationToken);
        }

        public async Task<bool> IsVerificationLockedOutAsync(string targetKey, CancellationToken cancellationToken = default)
        {
            var lockout = await _cache.GetStringAsync(GetVerificationLockoutKey(targetKey), cancellationToken);
            return !string.IsNullOrEmpty(lockout);
        }

        public async Task RecordVerificationLockoutAsync(
            string targetKey,
            TimeSpan lockoutPeriod,
            CancellationToken cancellationToken = default)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lockoutPeriod
            };

            await _cache.SetStringAsync(GetVerificationLockoutKey(targetKey), "1", options, cancellationToken);
        }

        public async Task ClearVerificationLockoutAsync(string targetKey, CancellationToken cancellationToken = default)
        {
            await _cache.RemoveAsync(GetVerificationLockoutKey(targetKey), cancellationToken);
        }
    }
}
