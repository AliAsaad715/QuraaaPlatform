using FluentValidation;

namespace Quraaa.Application.Features.Admin.Commands.DeleteOwnSuperAdminAccount
{
    public sealed class DeleteOwnSuperAdminAccountCommandValidator
        : AbstractValidator<DeleteOwnSuperAdminAccountCommand>
    {
        public DeleteOwnSuperAdminAccountCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Your password is required to confirm.");

            RuleFor(x => x.ConfirmationPhrase)
                .NotEmpty().WithMessage("The confirmation phrase is required.");
        }
    }
}
