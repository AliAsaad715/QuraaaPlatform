using FluentValidation;

namespace Quraaa.Application.Features.Payouts.Commands.CompleteLibraryWalletOnboarding
{
    public sealed class CompleteLibraryWalletOnboardingCommandValidator
        : AbstractValidator<CompleteLibraryWalletOnboardingCommand>
    {
        public CompleteLibraryWalletOnboardingCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User id is required.");
        }
    }
}
