using FluentValidation;
using Quraaa.Application.Features.Authentication.Common;

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
                .MinimumLength(AuthenticationPasswordPolicy.MinimumLength).WithMessage("Password is too short.")
                .MaximumLength(AuthenticationPasswordPolicy.MaximumLength).WithMessage("Password is too long.");
        }
    }
}
