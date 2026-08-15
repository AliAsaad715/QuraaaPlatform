namespace Quraaa.Domain.Library.Enums
{
    /// <summary>
    /// Readiness of a library's Stripe wallet (connected account) to receive
    /// profit-share transfers.
    /// </summary>
    public enum LibraryWalletStatus
    {
        /// <summary>No Stripe account is attached to the library.</summary>
        NotConnected = 1,

        /// <summary>
        /// A Stripe account was created for the library but its onboarding at
        /// Stripe is not finished, so it cannot receive transfers yet.
        /// </summary>
        OnboardingIncomplete = 2,

        /// <summary>The wallet can receive transfers.</summary>
        Active = 3,
    }
}
