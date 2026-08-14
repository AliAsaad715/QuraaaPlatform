using FluentValidation;

namespace Quraaa.Application.Features.Payouts.Commands.RemoveLibraryWallet
{
    public sealed class RemoveLibraryWalletCommandValidator
        : AbstractValidator<RemoveLibraryWalletCommand>
    {
        public RemoveLibraryWalletCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");
        }
    }
}
