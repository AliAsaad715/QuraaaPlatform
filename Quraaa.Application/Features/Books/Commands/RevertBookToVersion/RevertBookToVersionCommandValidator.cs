using FluentValidation;
using Quraaa.Domain.Catalog;

namespace Quraaa.Application.Features.Books.Commands.RevertBookToVersion
{
    public sealed class RevertBookToVersionCommandValidator
        : AbstractValidator<RevertBookToVersionCommand>
    {
        public RevertBookToVersionCommandValidator()
        {
            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");

            RuleFor(x => x.AdminId)
                .NotEmpty().WithMessage("Admin id is required.");

            RuleFor(x => x.VersionNumber)
                .GreaterThanOrEqualTo(1)
                .WithMessage("A book version number starts at 1.");

            RuleFor(x => x.ModerationNote)
                .MaximumLength(BookAggregate.MaxModerationNoteLength)
                .WithMessage($"The moderation note cannot exceed {BookAggregate.MaxModerationNoteLength} characters.");
        }
    }
}
