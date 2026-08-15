using FluentValidation;
using Quraaa.Application.Features.Libraries.Common;

namespace Quraaa.Application.Features.Libraries.Commands.ResetLibraryPassword
{
    public sealed class ResetLibraryPasswordCommandValidator
        : AbstractValidator<ResetLibraryPasswordCommand>
    {
        public ResetLibraryPasswordCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

            RuleFor(x => x.OtpCode)
                .NotEmpty().WithMessage("The verification code is required.")
                .Matches("^[0-9]{6}$").WithMessage("The verification code must be six digits.");

            RuleFor(x => x.NewPassword)
                .ApplyPasswordRules();

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty().WithMessage("Confirm the new library password.")
                .Equal(x => x.NewPassword)
                    .WithMessage("The new library password and its confirmation do not match.");
        }
    }
}
