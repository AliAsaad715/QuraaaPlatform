namespace Quraaa.Application.Features.Libraries.Common
{
    public sealed class LibraryRegistrationOptions
    {
        public required Uri DashboardRegisterUrl { get; init; }
        public TimeSpan MagicLinkLifetime { get; init; } = TimeSpan.FromMinutes(15);
        public TimeSpan SubmittedSessionLifetime { get; init; } = TimeSpan.FromHours(24);
        public TimeSpan EmailOtpLifetime { get; init; } = TimeSpan.FromMinutes(10);
        public TimeSpan EmailOtpResendCooldown { get; init; } = TimeSpan.FromMinutes(1);
        public TimeSpan EmailOtpSendWindow { get; init; } = TimeSpan.FromHours(1);
        public int MaxEmailOtpSendsPerWindow { get; init; } = 5;
        public int MaxEmailOtpVerificationAttempts { get; init; } = 5;
        public TimeSpan EmailOtpVerificationLockout { get; init; } = TimeSpan.FromMinutes(5);
    }
}
