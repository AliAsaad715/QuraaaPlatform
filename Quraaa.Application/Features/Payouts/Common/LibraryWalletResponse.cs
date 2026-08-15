using Quraaa.Domain.Library;
using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Payouts.Common
{
    /// <summary>
    /// The library owner's view of their Stripe wallet.
    /// </summary>
    /// <param name="StripeAccountId">The attached Stripe connected account, if any.</param>
    /// <param name="HasWallet">True when a Stripe account is attached (even if onboarding is unfinished).</param>
    /// <param name="WalletStatus">
    /// NotConnected, OnboardingIncomplete (attached but Stripe onboarding not
    /// finished — transfers wait), or Active (receives transfers).
    /// </param>
    /// <param name="ActivatedAtUtc">When the wallet was confirmed able to receive transfers.</param>
    /// <param name="ProfitSharePercent">The owner's share of gross sales set by the administrator.</param>
    public record LibraryWalletResponse(
        string? StripeAccountId,
        bool HasWallet,
        LibraryWalletStatus WalletStatus,
        DateTime? ActivatedAtUtc,
        decimal ProfitSharePercent)
    {
        public static LibraryWalletResponse From(LibraryAggregate library)
        {
            return new LibraryWalletResponse(
                library.StripeConnectAccountId,
                library.StripeConnectAccountId is not null,
                library.WalletStatus,
                library.StripeWalletActivatedAtUtc,
                library.ProfitSharePercent);
        }
    }
}
