using FluentValidation;

namespace Quraaa.Application.Features.Payouts.Commands.SetLibraryWallet
{
    public sealed class SetLibraryWalletCommandValidator
        : AbstractValidator<SetLibraryWalletCommand>
    {
        public SetLibraryWalletCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.StripeAccountId)
                .NotEmpty().WithMessage("Stripe account id is required.")
                .MaximumLength(255)
                .WithMessage("Stripe account id must not exceed 255 characters.")
                .Matches("^\\s*acct_[A-Za-z0-9]+\\s*$")
                .WithMessage(
                    "Stripe account id must be a connected account id such as acct_1ABC23DEF456.");
        }
    }
}
