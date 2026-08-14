namespace Quraaa.Application.Features.Payouts.Common
{
    /// <summary>
    /// Provider-neutral view of a Stripe connected account used to validate a
    /// library owner's wallet before storing or paying it.
    /// </summary>
    public sealed record PayoutConnectedAccountStatus(
        string AccountId,
        bool PayoutsEnabled,
        bool TransfersCapabilityActive,
        bool DetailsSubmitted)
    {
        // Stripe requires the destination account's "transfers" capability to
        // be active for it to receive a Transfer; payouts_enabled alone does
        // not guarantee that.
        public bool CanReceiveTransfers => TransfersCapabilityActive;
    }
}
