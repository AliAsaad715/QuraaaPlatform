using FluentValidation;

namespace Quraaa.Application.Features.Payouts.Commands.StartLibraryWalletOnboarding
{
    public sealed class StartLibraryWalletOnboardingCommandValidator
        : AbstractValidator<StartLibraryWalletOnboardingCommand>
    {
        public StartLibraryWalletOnboardingCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User id is required.");

            RuleFor(x => x.ReturnUrl)
                .MaximumLength(2048)
                .When(x => x.ReturnUrl is not null);

            RuleFor(x => x.RefreshUrl)
                .MaximumLength(2048)
                .When(x => x.RefreshUrl is not null);
        }
    }
}
