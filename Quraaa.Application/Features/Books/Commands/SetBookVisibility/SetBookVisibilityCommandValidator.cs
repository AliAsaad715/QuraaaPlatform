using FluentValidation;
using Quraaa.Domain.Catalog;

namespace Quraaa.Application.Features.Books.Commands.SetBookVisibility
{
    public sealed class SetBookVisibilityCommandValidator
        : AbstractValidator<SetBookVisibilityCommand>
    {
        public SetBookVisibilityCommandValidator()
        {
            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");

            RuleFor(x => x.AdminId)
                .NotEmpty().WithMessage("Admin id is required.");

            RuleFor(x => x.ModerationNote)
                .MaximumLength(BookAggregate.MaxModerationNoteLength)
                .WithMessage($"The moderation note cannot exceed {BookAggregate.MaxModerationNoteLength} characters.");
        }
    }
}
