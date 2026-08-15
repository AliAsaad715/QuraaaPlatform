using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Libraries.Commands.StartRegistrationStripeOnboarding
{
    /// <summary>
    /// Registration wizard step (registration token, after email
    /// verification): starts or resumes Stripe-hosted onboarding for the new
    /// library's wallet and returns the URL the dashboard must redirect to.
    /// </summary>
    public sealed record StartRegistrationStripeOnboardingCommand(
        string Token,
        string? ReturnUrl,
        string? RefreshUrl)
        : IRequest<AppResult<LibraryStripeOnboardingResponse>>;
}
