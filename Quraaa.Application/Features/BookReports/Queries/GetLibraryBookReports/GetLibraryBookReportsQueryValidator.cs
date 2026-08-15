using FluentValidation;

namespace Quraaa.Application.Features.BookReports.Queries.GetLibraryBookReports
{
    public sealed class GetLibraryBookReportsQueryValidator
        : AbstractValidator<GetLibraryBookReportsQuery>
    {
        public GetLibraryBookReportsQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue);
        }
    }
}
