using Quraaa.Domain.Library.Enums;

namespace Quraaa.Application.Features.Payouts.Common
{
    /// <summary>
    /// What the payout gateway needs to create a connected account (wallet)
    /// for a library owner through provider-hosted onboarding.
    /// </summary>
    /// <param name="Email">The library's verified contact email; prefilled at the provider.</param>
    /// <param name="BusinessName">The library's display name; prefilled at the provider.</param>
    /// <param name="LibraryId">Stored as provider metadata for traceability.</param>
    /// <param name="OwnerUserId">Stored as provider metadata for traceability.</param>
    /// <param name="IdempotencyKey">
    /// Provider idempotency key so a crash between account creation and our
    /// save does not create a second account on retry.
    /// </param>
    public sealed record PayoutConnectedAccountRequest(
        string Email,
        string BusinessName,
        Guid LibraryId,
        Guid OwnerUserId,
        string IdempotencyKey);

    /// <summary>A short-lived provider-hosted onboarding link.</summary>
    public sealed record PayoutOnboardingLink(
        string Url,
        DateTime ExpiresAtUtc);

    /// <summary>
    /// Returned when a library owner starts (or resumes) Stripe onboarding:
    /// the client must redirect the owner to <paramref name="OnboardingUrl"/>.
    /// </summary>
    public sealed record LibraryStripeOnboardingResponse(
        string OnboardingUrl,
        DateTime ExpiresAtUtc,
        string StripeAccountId,
        LibraryWalletStatus WalletStatus);

    /// <summary>
    /// A short-lived link to the owner's Stripe Express dashboard, where the
    /// owner edits bank details and sees Stripe-side balances/payouts.
    /// </summary>
    public sealed record LibraryWalletDashboardLinkResponse(string Url);
}
