using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Commands.RequestLibraryPasswordReset
{
    public sealed class RequestLibraryPasswordResetCommandValidator
        : AbstractValidator<RequestLibraryPasswordResetCommand>
    {
        public RequestLibraryPasswordResetCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");
        }
    }
}
