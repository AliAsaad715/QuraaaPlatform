using FluentValidation;
using Quraaa.Application.Features.Authentication.Common;

namespace Quraaa.Application.Features.Admin.Commands.CreateSuperAdmin
{
    public sealed class CreateSuperAdminCommandValidator
        : AbstractValidator<CreateSuperAdminCommand>
    {
        public CreateSuperAdminCommandValidator()
        {
            RuleFor(x => x.CreatedByUserId)
                .NotEmpty().WithMessage("The creating user is required.");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100);

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .MaximumLength(20);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(AuthenticationPasswordPolicy.MinimumLength)
                    .WithMessage($"Password must be at least {AuthenticationPasswordPolicy.MinimumLength} characters long.")
                .MaximumLength(AuthenticationPasswordPolicy.MaximumLength)
                .Must(AuthenticationPasswordPolicy.MeetsComplexityRequirements)
                    .WithMessage("Password must contain an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character.");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Confirm the password.")
                .Equal(x => x.Password)
                    .WithMessage("The password and its confirmation do not match.");
        }
    }
}
