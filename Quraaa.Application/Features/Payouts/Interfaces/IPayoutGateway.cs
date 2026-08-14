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
    }
}
