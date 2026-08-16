using FluentValidation;

namespace Quraaa.Application.Features.Authors.Queries.SearchAuthors
{
    public class SearchAuthorsQueryValidator : AbstractValidator<SearchAuthorsQuery>
    {
        public SearchAuthorsQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        }
    }
}
