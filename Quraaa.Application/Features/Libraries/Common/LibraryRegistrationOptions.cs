namespace Quraaa.Application.Features.Libraries.Common
{
    public sealed class LibraryRegistrationOptions
    {
        public required Uri DashboardRegisterUrl { get; init; }

        /// <summary>
        /// Origins (scheme://host[:port]) that Stripe-hosted onboarding may
        /// redirect library owners back to. Always includes the dashboard
        /// origin; the API layer adds any configured frontend origins.
        /// </summary>
        public IReadOnlyCollection<string> AllowedReturnOrigins { get; init; } = Array.Empty<string>();
        public TimeSpan MagicLinkLifetime { get; init; } = TimeSpan.FromMinutes(15);
        public TimeSpan SubmittedSessionLifetime { get; init; } = TimeSpan.FromHours(24);

        /// <summary>
        /// How long the wizard may still be used for the optional Stripe wallet
        /// step after the email is verified. Deliberately much shorter than
        /// <see cref="SubmittedSessionLifetime"/>: from this point the token can
        /// bind the payout account, and an owner who needs more time can always
        /// get a fresh link (or connect the wallet from the dashboard after
        /// approval).
        /// </summary>
        public TimeSpan WalletSetupSessionLifetime { get; init; } = TimeSpan.FromHours(2);
        public TimeSpan EmailOtpLifetime { get; init; } = TimeSpan.FromMinutes(10);
        public TimeSpan EmailOtpResendCooldown { get; init; } = TimeSpan.FromMinutes(1);
        public TimeSpan EmailOtpSendWindow { get; init; } = TimeSpan.FromHours(1);
        public int MaxEmailOtpSendsPerWindow { get; init; } = 5;
        public int MaxEmailOtpVerificationAttempts { get; init; } = 5;
        public TimeSpan EmailOtpVerificationLockout { get; init; } = TimeSpan.FromMinutes(5);
    }
}
