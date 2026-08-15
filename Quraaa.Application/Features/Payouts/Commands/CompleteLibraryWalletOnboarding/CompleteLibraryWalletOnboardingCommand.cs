using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Payouts.Commands.CompleteLibraryWalletOnboarding
{
    /// <summary>
    /// Library owner (JWT): called when the owner returns from Stripe-hosted
    /// onboarding. Re-checks the wallet with Stripe and activates it if it
    /// can now receive transfers. Idempotent.
    /// </summary>
    public record CompleteLibraryWalletOnboardingCommand(Guid UserId)
        : IRequest<AppResult<LibraryWalletResponse>>;
}
