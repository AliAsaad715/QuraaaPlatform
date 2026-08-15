namespace Quraaa.Application.Features.Payouts.Common
{
    /// <summary>
    /// A profit-share transfer to a seller's connected account.
    /// </summary>
    /// <param name="DestinationAccountId">The seller's connected account.</param>
    /// <param name="AmountMinor">Amount in minor units.</param>
    /// <param name="Currency">ISO currency code.</param>
    /// <param name="TransferGroup">Groups every transfer of one order.</param>
    /// <param name="IdempotencyKey">Provider idempotency key for this attempt.</param>
    /// <param name="Metadata">Traceability metadata stored on the transfer.</param>
    /// <param name="SourceTransactionId">
    /// The charge that funds this transfer. When set, the provider draws on
    /// that specific payment instead of the platform's available balance, so
    /// the transfer succeeds even while the charge is still settling.
    /// </param>
    public sealed record PayoutTransferRequest(
        string DestinationAccountId,
        long AmountMinor,
        string Currency,
        string TransferGroup,
        string IdempotencyKey,
        IReadOnlyDictionary<string, string> Metadata,
        string? SourceTransactionId = null);
}
