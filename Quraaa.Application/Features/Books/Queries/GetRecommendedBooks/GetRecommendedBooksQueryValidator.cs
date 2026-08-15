using FluentValidation;
using Quraaa.Domain.Catalog.Enums;

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
                .NotNull().WithMessage("Language is required.")
                .Must(language => language is Language.Arabic or Language.English)
                .WithMessage("Language must be either Arabic or English.");

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
