using FluentValidation;
using PhoneNumbers;

namespace Quraaa.Application.Features.Otp.Commands.SendOtp
{
    public class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
    {
        public SendOtpCommandValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(BeAValidInternationalPhoneNumber)
                .WithMessage("Invalid international phone number format. It must start with '+' and include a valid country code.");

            RuleFor(x => x.SmsGatewayDeviceToken)
                .NotEmpty().WithMessage("SMS gateway device token is required.")
                .MaximumLength(4096).WithMessage("SMS gateway device token is too long.");
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
