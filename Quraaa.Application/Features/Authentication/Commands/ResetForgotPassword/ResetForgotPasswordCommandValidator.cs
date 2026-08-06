using FluentValidation;
using PhoneNumbers;
using Quraaa.Application.Features.Authentication.Common;

namespace Quraaa.Application.Features.Authentication.Commands.ResetForgotPassword
{
    public class ResetForgotPasswordCommandValidator : AbstractValidator<ResetForgotPasswordCommand>
    {
        public ResetForgotPasswordCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(BeAValidInternationalPhoneNumber)
                .WithMessage("Invalid international phone number format. It must start with '+' and include a valid country code.");

            RuleFor(x => x.OtpCode)
                .NotEmpty().WithMessage("OTP code is required.")
                .Length(6).WithMessage("OTP code must be exactly 6 digits.")
                .Matches("^[0-9]+$").WithMessage("OTP code must contain only digits.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(AuthenticationPasswordPolicy.MinimumLength)
                    .WithMessage($"New password must be at least {AuthenticationPasswordPolicy.MinimumLength} characters long.")
                .MaximumLength(AuthenticationPasswordPolicy.MaximumLength)
                    .WithMessage($"New password must not exceed {AuthenticationPasswordPolicy.MaximumLength} characters.")
                .Must(AuthenticationPasswordPolicy.MeetsComplexityRequirements)
                    .WithMessage("New password must contain an uppercase letter, a lowercase letter, a digit, and a non-alphanumeric character.");
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
