namespace Quraaa.Application.Features.Payouts.Common
{
    /// <summary>
    /// Bound to the "Payouts" configuration section in the API layer.
    /// The profit-share percentage itself is not configured here: it is set
    /// per library by administrators (see LibraryAggregate.ProfitSharePercent).
    /// </summary>
    public sealed class PayoutOptions
    {
        /// <summary>
        /// How many Stripe transfer attempts a payout may consume before it is
        /// marked <c>Failed</c> and left for manual review.
        /// </summary>
        public int MaxTransferAttempts { get; set; } = 10;
    }
}
