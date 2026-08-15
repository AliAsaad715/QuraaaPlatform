using FluentValidation;
using Quraaa.Application.Features.Libraries.Common;

namespace Quraaa.Application.Features.Libraries.Commands.ChangeLibraryPassword
{
    public sealed class ChangeLibraryPasswordCommandValidator
        : AbstractValidator<ChangeLibraryPasswordCommand>
    {
        public ChangeLibraryPasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("The current library password is required.");

            RuleFor(x => x.NewPassword)
                .ApplyPasswordRules()
                .NotEqual(x => x.CurrentPassword)
                    .WithMessage("The new library password must be different from the current one.");

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Confirm the new library password.")
                .Equal(x => x.NewPassword)
                    .WithMessage("The new library password and its confirmation do not match.");
        }
    }
}
