namespace Quraaa.Application.Features.Payouts.Common
{
    public record LibraryWalletResponse(
        string? StripeAccountId,
        bool HasWallet);
}
