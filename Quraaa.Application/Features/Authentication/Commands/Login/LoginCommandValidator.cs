using FluentValidation;
using PhoneNumbers;
using Quraaa.Application.Features.Authentication.Common;

namespace Quraaa.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(BeASyrianPhoneNumber)
                .WithMessage("Invalid Syrian phone number. It must start with '+963' and be a valid Syrian number.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(AuthenticationPasswordPolicy.MinimumLength)
                    .WithMessage($"Password must be at least {AuthenticationPasswordPolicy.MinimumLength} characters long.")
                .MaximumLength(AuthenticationPasswordPolicy.MaximumLength)
                    .WithMessage($"Password must not exceed {AuthenticationPasswordPolicy.MaximumLength} characters.");
        }

        private bool BeASyrianPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            var trimmed = phoneNumber.Trim();
            if (!trimmed.StartsWith("+963"))
            {
                return false;
            }

            var phoneUtil = PhoneNumberUtil.GetInstance();
            try
            {
                var number = phoneUtil.Parse(trimmed, "SY");
                return phoneUtil.IsValidNumberForRegion(number, "SY");
            }
            catch (NumberParseException)
            {
                return false;
            }
        }
    }
}
