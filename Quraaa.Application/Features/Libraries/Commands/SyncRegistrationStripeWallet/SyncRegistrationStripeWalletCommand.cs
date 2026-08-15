using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Commands.SyncRegistrationStripeWallet
{
    /// <summary>
    /// Registration wizard step (registration token): called when the owner
    /// returns from Stripe-hosted onboarding. Re-checks the wallet with Stripe,
    /// activates it if ready, and completes the registration session once the
    /// wallet is active. Idempotent.
    /// </summary>
    public sealed record SyncRegistrationStripeWalletCommand(string Token)
        : IRequest<AppResult<LibraryWalletResponse>>;
}
