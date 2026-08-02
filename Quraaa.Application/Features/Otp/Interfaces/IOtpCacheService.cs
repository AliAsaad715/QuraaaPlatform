namespace Quraaa.Application.Features.Otp.Interfaces
{
    public interface IOtpCacheService
    {
        Task SetOtpAsync(string phoneNumber, string otpCode, TimeSpan expiration, string keyPrefix, CancellationToken cancellationToken = default);
        Task<string?> GetOtpAsync(string phoneNumber, string keyPrefix, CancellationToken cancellationToken = default);
        Task ClearOtpAsync(string phoneNumber, string keyPrefix, CancellationToken cancellationToken = default);

        Task<bool> HasRecentOtpRequestAsync(string phoneNumber, TimeSpan lockoutPeriod, string keyPrefix, CancellationToken cancellationToken = default);
        Task RecordOtpRequestAsync(string phoneNumber, TimeSpan lockoutPeriod, string keyPrefix, CancellationToken cancellationToken = default);
        Task ClearOtpRequestAsync(string phoneNumber, string keyPrefix, CancellationToken cancellationToken = default);
        Task<bool> TryAcquireOtpRequestAsync(string targetKey, string ownerToken, TimeSpan lockoutPeriod, string keyPrefix, CancellationToken cancellationToken = default);
        Task<bool> TrySetOwnedOtpAsync(string phoneNumber, string otpCode, string ownerToken, TimeSpan expiration, string keyPrefix, CancellationToken cancellationToken = default);
        Task ClearOwnedOtpAsync(string phoneNumber, string otpCode, string ownerToken, string keyPrefix, CancellationToken cancellationToken = default);
        Task ReleaseOtpRequestAsync(string targetKey, string ownerToken, string keyPrefix, CancellationToken cancellationToken = default);

        Task<int> IncrementFailedVerificationAttemptAsync(string targetKey, TimeSpan expiration, string keyPrefix, CancellationToken cancellationToken = default);
        Task ClearFailedVerificationAttemptsAsync(string targetKey, string keyPrefix, CancellationToken cancellationToken = default);
        Task<bool> IsVerificationLockedOutAsync(string targetKey, string keyPrefix, CancellationToken cancellationToken = default);
        Task RecordVerificationLockoutAsync(string targetKey, TimeSpan lockoutPeriod, string keyPrefix, CancellationToken cancellationToken = default);
        Task ClearVerificationLockoutAsync(string targetKey, string keyPrefix, CancellationToken cancellationToken = default);
    }
}
