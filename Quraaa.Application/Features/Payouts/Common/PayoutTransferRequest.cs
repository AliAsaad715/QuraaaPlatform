namespace Quraaa.Application.Features.Payouts.Common
{
    public sealed record PayoutTransferRequest(
        string DestinationAccountId,
        long AmountMinor,
        string Currency,
        string TransferGroup,
        string IdempotencyKey,
        IReadOnlyDictionary<string, string> Metadata);
}
