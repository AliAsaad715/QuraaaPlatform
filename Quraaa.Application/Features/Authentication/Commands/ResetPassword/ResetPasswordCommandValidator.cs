using FluentValidation;
using Quraaa.Application.Features.Authentication.Common;

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
                .MinimumLength(AuthenticationPasswordPolicy.MinimumLength)
                    .WithMessage($"Old password must be at least {AuthenticationPasswordPolicy.MinimumLength} characters long.")
                .MaximumLength(AuthenticationPasswordPolicy.MaximumLength)
                    .WithMessage($"Old password must not exceed {AuthenticationPasswordPolicy.MaximumLength} characters.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(AuthenticationPasswordPolicy.MinimumLength)
                    .WithMessage($"New password must be at least {AuthenticationPasswordPolicy.MinimumLength} characters long.")
                .MaximumLength(AuthenticationPasswordPolicy.MaximumLength)
                    .WithMessage($"New password must not exceed {AuthenticationPasswordPolicy.MaximumLength} characters.")
                .Must(AuthenticationPasswordPolicy.MeetsComplexityRequirements)
                    .WithMessage("New password must contain an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character.")
                .NotEqual(x => x.OldPassword).WithMessage("New password must be different from old password.");
        }
    }
}
