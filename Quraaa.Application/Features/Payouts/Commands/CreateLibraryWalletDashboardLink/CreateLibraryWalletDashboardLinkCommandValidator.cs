using FluentValidation;

namespace Quraaa.Application.Features.Payouts.Commands.CreateLibraryWalletDashboardLink
{
    public sealed class CreateLibraryWalletDashboardLinkCommandValidator
        : AbstractValidator<CreateLibraryWalletDashboardLinkCommand>
    {
        public CreateLibraryWalletDashboardLinkCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("User id is required.");
        }
    }
}
