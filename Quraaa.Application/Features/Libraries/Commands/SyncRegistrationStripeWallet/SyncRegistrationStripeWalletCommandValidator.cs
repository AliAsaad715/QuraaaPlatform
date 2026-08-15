using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Commands.SyncRegistrationStripeWallet
{
    public sealed class SyncRegistrationStripeWalletCommandValidator
        : AbstractValidator<SyncRegistrationStripeWalletCommand>
    {
        public SyncRegistrationStripeWalletCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .WithMessage("Registration token is required.")
                .MaximumLength(128);
        }
    }
}
