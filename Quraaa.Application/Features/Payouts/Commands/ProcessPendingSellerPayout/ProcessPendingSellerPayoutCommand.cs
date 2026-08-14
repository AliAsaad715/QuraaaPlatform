using MediatR;

namespace Quraaa.Application.Features.Payouts.Commands.ProcessPendingSellerPayout
{
    /// <summary>
    /// Attempts to move one pending seller payout through the payout gateway.
    /// Sent by the background payout processor; returns true when the payout
    /// was paid during this attempt.
    /// </summary>
    public sealed record ProcessPendingSellerPayoutCommand(Guid PayoutId)
        : IRequest<bool>;
}
