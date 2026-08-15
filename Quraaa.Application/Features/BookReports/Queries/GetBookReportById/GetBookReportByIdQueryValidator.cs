using FluentValidation;

namespace Quraaa.Application.Features.BookReports.Queries.GetBookReportById
{
    public sealed class GetBookReportByIdQueryValidator
        : AbstractValidator<GetBookReportByIdQuery>
    {
        public GetBookReportByIdQueryValidator()
        {
            RuleFor(x => x.ReportId)
                .NotEmpty().WithMessage("Report id is required.");
        }
    }
}
