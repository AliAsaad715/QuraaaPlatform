using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Commands.ResendLibraryEmailOtp
{
    public sealed class ResendLibraryEmailOtpCommandValidator
        : AbstractValidator<ResendLibraryEmailOtpCommand>
    {
        public ResendLibraryEmailOtpCommandValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty()
                .MaximumLength(128);
        }
    }
}
