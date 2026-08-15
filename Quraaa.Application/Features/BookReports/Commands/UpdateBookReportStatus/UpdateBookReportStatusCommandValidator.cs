using FluentValidation;
using Quraaa.Domain.Reports;
using Quraaa.Domain.Reports.Enums;

namespace Quraaa.Application.Features.BookReports.Commands.UpdateBookReportStatus
{
    public sealed class UpdateBookReportStatusCommandValidator
        : AbstractValidator<UpdateBookReportStatusCommand>
    {
        public UpdateBookReportStatusCommandValidator()
        {
            RuleFor(x => x.ReportId)
                .NotEmpty().WithMessage("Report id is required.");

            RuleFor(x => x.AdminId)
                .NotEmpty().WithMessage("Admin id is required.");

            RuleFor(x => x.Status)
                .IsInEnum().WithMessage("Invalid report status.")
                .Must(status => status != BookReportStatus.Pending)
                .WithMessage("Status must be InReview, Resolved, or Rejected.");

            RuleFor(x => x.ModeratorNote)
                .MaximumLength(BookReportAggregate.MaxModeratorNoteLength)
                .WithMessage($"The moderator note cannot exceed {BookReportAggregate.MaxModeratorNoteLength} characters.");
        }
    }
}
