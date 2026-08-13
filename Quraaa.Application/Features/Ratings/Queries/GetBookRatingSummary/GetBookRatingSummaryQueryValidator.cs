using FluentValidation;

namespace Quraaa.Application.Features.Ratings.Queries.GetBookRatingSummary
{
    public class GetBookRatingSummaryQueryValidator : AbstractValidator<GetBookRatingSummaryQuery>
    {
        public GetBookRatingSummaryQueryValidator()
        {
            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");
        }
    }
}
