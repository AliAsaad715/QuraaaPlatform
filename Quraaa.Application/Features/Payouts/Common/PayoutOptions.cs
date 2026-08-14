namespace Quraaa.Application.Features.Payouts.Common
{
    /// <summary>
    /// Bound to the "Payouts" configuration section in the API layer.
    /// </summary>
    public sealed class PayoutOptions
    {
        /// <summary>
        /// The percentage of a library seller's gross sale amount kept by the
        /// platform. The library owner receives the remainder.
        /// </summary>
        public decimal PlatformCommissionPercent { get; set; } = 10m;

        /// <summary>
        /// How many Stripe transfer attempts a payout may consume before it is
        /// marked <c>Failed</c> and left for manual review.
        /// </summary>
        public int MaxTransferAttempts { get; set; } = 10;
    }
}
