using FluentValidation;

namespace Quraaa.Application.Features.BookReports.Queries.GetBookReports
{
    public sealed class GetBookReportsQueryValidator : AbstractValidator<GetBookReportsQuery>
    {
        public GetBookReportsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x.Status)
                .IsInEnum()
                .When(x => x.Status.HasValue);

            RuleFor(x => x.SearchTerm)
                .MaximumLength(200)
                .WithMessage("The search term cannot exceed 200 characters.");
        }
    }
}
