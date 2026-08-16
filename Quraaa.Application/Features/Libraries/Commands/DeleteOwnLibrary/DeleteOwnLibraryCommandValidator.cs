using FluentValidation;

namespace Quraaa.Application.Features.Libraries.Commands.DeleteOwnLibrary
{
    public sealed class DeleteOwnLibraryCommandValidator
        : AbstractValidator<DeleteOwnLibraryCommand>
    {
        public DeleteOwnLibraryCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("The library password is required to confirm.");

            RuleFor(x => x.ConfirmationPhrase)
                .NotEmpty().WithMessage("The confirmation phrase is required.");
        }
    }
}
