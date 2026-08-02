using FluentValidation;

namespace Quraaa.Application.Features.Books.Queries.GetRecommendedBooks
{
    public class GetRecommendedBooksQueryValidator : AbstractValidator<GetRecommendedBooksQuery>
    {
        public GetRecommendedBooksQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.Language)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Language is required.")
                .MaximumLength(20).WithMessage("Language must be 20 characters or fewer.")
                .Must(language =>
                    language.Equals("ar", StringComparison.OrdinalIgnoreCase) ||
                    language.Equals("en", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Language must be either 'ar' or 'en'.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

            RuleFor(x => x.SearchTerm)
                .MaximumLength(100).When(x => x.SearchTerm is not null)
                .WithMessage("Search term must be 100 characters or fewer.");
        }
    }
}
