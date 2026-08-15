using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Commands.StartRegistrationStripeOnboarding
{
    public sealed class StartRegistrationStripeOnboardingCommandValidator
        : AbstractValidator<StartRegistrationStripeOnboardingCommand>
    {
        public StartRegistrationStripeOnboardingCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Registration token is required.")
                .MaximumLength(128);

            RuleFor(x => x.ReturnUrl)
                .MaximumLength(2048)
                .When(x => x.ReturnUrl is not null);

            RuleFor(x => x.RefreshUrl)
                .MaximumLength(2048)
                .When(x => x.RefreshUrl is not null);
        }
    }
}
