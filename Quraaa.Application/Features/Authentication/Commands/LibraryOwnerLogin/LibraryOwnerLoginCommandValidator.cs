using FluentValidation;

namespace Quraaa.Application.Features.Authentication.Commands.LibraryOwnerLogin
{
    public class LibraryOwnerLoginCommandValidator : AbstractValidator<LibraryOwnerLoginCommand>
    {
        public LibraryOwnerLoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Library email is required.")
                .MaximumLength(256).WithMessage("Library email is too long.")
                .EmailAddress().WithMessage("Invalid library email format.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MaximumLength(256).WithMessage("Password is too long.");
        }
    }
}
