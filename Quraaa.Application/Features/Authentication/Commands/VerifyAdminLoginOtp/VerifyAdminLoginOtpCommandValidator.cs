using FluentValidation;
using PhoneNumbers;

namespace Quraaa.Application.Features.Authentication.Commands.VerifyAdminLoginOtp
{
    public class VerifyAdminLoginOtpCommandValidator : AbstractValidator<VerifyAdminLoginOtpCommand>
    {
        public VerifyAdminLoginOtpCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(BeAValidInternationalPhoneNumber)
                .WithMessage("Invalid international phone number format. It must start with '+' and include a valid country code.");

            RuleFor(x => x.OtpCode)
                .NotEmpty().WithMessage("OTP code is required.")
                .Length(6).WithMessage("OTP code must be exactly 6 digits.")
                .Matches("^[0-9]+$").WithMessage("OTP code must contain only digits.");
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
