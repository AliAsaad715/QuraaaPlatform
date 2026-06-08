using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Commands.RegisterLibrary
{
    public class RegisterLibraryCommandValidator : AbstractValidator<RegisterLibraryCommand>
    {
        public RegisterLibraryCommandValidator()
        {
            RuleFor(x => x.LibraryName)
                .NotEmpty().WithMessage("Library name is required.")
                .MaximumLength(100).WithMessage("Library name must not exceed 100 characters.");

            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("Location is required.")
                .MaximumLength(250).WithMessage("Location must not exceed 250 characters.");

            RuleFor(x => x.LibraryImage)
                .NotEmpty().WithMessage("Library image is required.")
                .MaximumLength(500).WithMessage("Library image must not exceed 500 characters.");

            RuleFor(x => x.HeaderImage)
                .NotEmpty().WithMessage("Header image is required.")
                .MaximumLength(500).WithMessage("Header image must not exceed 500 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email format is invalid.")
                .MaximumLength(256).WithMessage("Email must not exceed 256 characters.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");
        }
    }
}
