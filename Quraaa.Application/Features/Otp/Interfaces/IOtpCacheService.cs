namespace Quraaa.Application.Features.Otp.Interfaces
{
    public interface IOtpCacheService
    {
        Task SetOtpAsync(string phoneNumber, string otpCode, TimeSpan expiration, CancellationToken cancellationToken = default);
        Task<string?> GetOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);
        Task ClearOtpAsync(string phoneNumber, CancellationToken cancellationToken = default);

        Task<bool> HasRecentOtpRequestAsync(string phoneNumber, TimeSpan lockoutPeriod, CancellationToken cancellationToken = default);
        Task RecordOtpRequestAsync(string phoneNumber, TimeSpan lockoutPeriod, CancellationToken cancellationToken = default);
        Task ClearOtpRequestAsync(string phoneNumber, CancellationToken cancellationToken = default);

        Task<int> IncrementFailedVerificationAttemptAsync(string targetKey, TimeSpan expiration, CancellationToken cancellationToken = default);
        Task ClearFailedVerificationAttemptsAsync(string targetKey, CancellationToken cancellationToken = default);
        Task<bool> IsVerificationLockedOutAsync(string targetKey, CancellationToken cancellationToken = default);
        Task RecordVerificationLockoutAsync(string targetKey, TimeSpan lockoutPeriod, CancellationToken cancellationToken = default);
        Task ClearVerificationLockoutAsync(string targetKey, CancellationToken cancellationToken = default);
    }
}
