using FluentValidation;

namespace Quraaa.Application.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.OldPassword)
                .NotEmpty().WithMessage("Old password is required.")
                .MinimumLength(8).WithMessage("Old password must be at least 8 characters long.")
                .MaximumLength(64).WithMessage("Old password must not exceed 64 characters.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
                .MaximumLength(64).WithMessage("New password must not exceed 64 characters.")
                .NotEqual(x => x.OldPassword).WithMessage("New password must be different from old password.");
        }
    }
}
