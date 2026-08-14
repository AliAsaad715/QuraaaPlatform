namespace Quraaa.Application.Features.Payouts.Common
{
    /// <summary>
    /// A transfer found at the provider that already belongs to a payout —
    /// used to adopt work that completed before a crash instead of paying
    /// twice.
    /// </summary>
    public sealed record PayoutExistingTransfer(
        string TransferId,
        string DestinationAccountId);
}
