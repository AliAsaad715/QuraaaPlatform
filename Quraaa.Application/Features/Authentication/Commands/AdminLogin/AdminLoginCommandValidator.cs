using FluentValidation;
using PhoneNumbers;

namespace Quraaa.Application.Features.Authentication.Commands.AdminLogin
{
    public class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
    {
        public AdminLoginCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(BeAValidInternationalPhoneNumber)
                .WithMessage("Invalid international phone number format. It must start with '+' and include a valid country code.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MinimumLength(6).WithMessage("Password is too short.")
                .MaximumLength(256).WithMessage("Password is too long.");
        }

        private bool BeAValidInternationalPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;
            if (!phoneNumber.Trim().StartsWith("+")) return false;

            var phoneUtil = PhoneNumberUtil.GetInstance();
            try
            {
                var number = phoneUtil.Parse(phoneNumber, null);
                return phoneUtil.IsValidNumber(number);
            }
            catch (NumberParseException)
            {
                return false;
            }
        }
    }
}
