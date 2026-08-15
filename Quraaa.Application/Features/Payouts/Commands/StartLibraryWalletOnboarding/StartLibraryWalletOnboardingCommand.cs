using MediatR;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Results;
using System.Text.Json.Serialization;

namespace Quraaa.Application.Features.Payouts.Commands.StartLibraryWalletOnboarding
{
    /// <summary>
    /// Library owner (approved library, JWT): starts or resumes Stripe-hosted
    /// onboarding for the library's wallet and returns the URL to redirect to.
    /// </summary>
    public record StartLibraryWalletOnboardingCommand : IRequest<AppResult<LibraryStripeOnboardingResponse>>
    {
        [JsonIgnore]
        public Guid UserId { get; init; }

        /// <summary>
        /// Where Stripe sends the owner after onboarding. Optional; must be on
        /// an allow-listed frontend origin. Defaults to the dashboard URL.
        /// </summary>
        public string? ReturnUrl { get; init; }

        /// <summary>
        /// Where Stripe sends the owner if the onboarding link expired.
        /// Optional; same rules as <see cref="ReturnUrl"/>.
        /// </summary>
        public string? RefreshUrl { get; init; }
    }
}
