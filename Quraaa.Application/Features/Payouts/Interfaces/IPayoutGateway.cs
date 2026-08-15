using Quraaa.Application.Features.Payouts.Common;

namespace Quraaa.Application.Features.Payouts.Interfaces
{
    /// <summary>
    /// Payment-provider operations needed to move seller profit shares to
    /// connected accounts. Implemented by the Stripe Connect gateway in
    /// Infrastructure.
    /// </summary>
    public interface IPayoutGateway
    {
        /// <summary>
        /// Retrieves a connected account's payout readiness. Returns
        /// <see langword="null"/> when the account does not exist or is not
        /// connected to this platform.
        /// </summary>
        Task<PayoutConnectedAccountStatus?> GetConnectedAccountAsync(
            string stripeAccountId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the charge that a successful payment produced, so a
        /// transfer can be funded directly from it. Returns
        /// <see langword="null"/> when the payment has no charge yet.
        /// </summary>
        Task<string?> ResolveChargeIdAsync(
            string paymentIntentId,
            CancellationToken cancellationToken = default);

        Task<PayoutTransferResult> CreateTransferAsync(
            PayoutTransferRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Looks for an existing transfer in the given transfer group whose
        /// metadata ties it to the given payout id. Returns
        /// <see langword="null"/> when none exists. Used before creating a
        /// transfer for a stale payout whose idempotency key may have expired
        /// at the provider.
        /// </summary>
        Task<PayoutExistingTransfer?> FindTransferForPayoutAsync(
            string transferGroup,
            Guid sellerPayoutId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new connected account (Stripe Express) under the platform
        /// for a library owner and returns its id. Onboarding is completed by
        /// the owner at the provider via <see cref="CreateOnboardingLinkAsync"/>.
        /// </summary>
        Task<string> CreateConnectedAccountAsync(
            PayoutConnectedAccountRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a single-use, short-lived provider-hosted onboarding link
        /// for the connected account. The owner is redirected there and comes
        /// back to <paramref name="returnUrl"/> when done, or to
        /// <paramref name="refreshUrl"/> if the link expired.
        /// </summary>
        Task<PayoutOnboardingLink> CreateOnboardingLinkAsync(
            string stripeAccountId,
            Uri returnUrl,
            Uri refreshUrl,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a short-lived login link to the connected account's
        /// provider-hosted (Stripe Express) dashboard, where the owner manages
        /// bank details. Returns <see langword="null"/> when the account type
        /// does not support hosted dashboard links.
        /// </summary>
        Task<string?> CreateExpressDashboardLinkAsync(
            string stripeAccountId,
            CancellationToken cancellationToken = default);
    }
}
