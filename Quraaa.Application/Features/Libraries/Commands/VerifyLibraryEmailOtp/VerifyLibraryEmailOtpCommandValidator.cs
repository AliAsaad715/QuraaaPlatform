using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Commands.VerifyLibraryEmailOtp
{
    public sealed class VerifyLibraryEmailOtpCommandValidator
        : AbstractValidator<VerifyLibraryEmailOtpCommand>
    {
        public VerifyLibraryEmailOtpCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .MaximumLength(128);

            RuleFor(x => x.VerificationId)
                .NotEmpty();

            RuleFor(x => x.OtpCode)
                .NotEmpty()
                .Matches("^[0-9]{6}$")
                .WithMessage("OTP code must contain exactly six ASCII digits.");
        }
    }
}
