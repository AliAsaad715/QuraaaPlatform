using FluentValidation;
using Quraaa.Application.Features.BookReports.Common;
using Quraaa.Domain.Reports;

namespace Quraaa.Application.Features.BookReports.Commands.CreateBookReport
{
    public sealed class CreateBookReportCommandValidator
        : AbstractValidator<CreateBookReportCommand>
    {
        public CreateBookReportCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");

            RuleFor(x => x.Reason)
                .IsInEnum().WithMessage("Select one of the predefined report reasons.");

            RuleFor(x => x.Details)
                .MaximumLength(BookReportAggregate.MaxDetailsLength)
                .WithMessage($"Report details cannot exceed {BookReportAggregate.MaxDetailsLength} characters.");

            RuleFor(x => x.Details)
                .NotEmpty()
                .When(x => BookReportReasonCatalog.RequiresDetails(x.Reason))
                .WithMessage("Describe the problem when the reason is Other.");
        }
    }
}
