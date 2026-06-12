using FluentValidation;

namespace Quraaa.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(phone => !string.IsNullOrWhiteSpace(phone) && phone.Trim().StartsWith("+"))
                .WithMessage("Invalid phone number format. It must start with '+'");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}