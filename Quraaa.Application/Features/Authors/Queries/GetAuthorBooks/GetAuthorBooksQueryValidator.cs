using FluentValidation;

namespace Quraaa.Application.Features.Authors.Queries.GetAuthorBooks
{
    public sealed class GetAuthorBooksQueryValidator : AbstractValidator<GetAuthorBooksQuery>
    {
        private static readonly IReadOnlySet<string> AllowedSortFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "latest",
                "bestselling",
                "mostpopular",
                "pricelowtohigh",
                "pricehightolow",
                "toprated"
            };

        public GetAuthorBooksQueryValidator()
        {
            RuleFor(query => query.AuthorId)
                .NotEmpty().WithMessage("Author id is required.");

            RuleFor(query => query.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(query => query.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

            RuleFor(query => query.SearchTerm)
                .MaximumLength(100).When(query => query.SearchTerm is not null)
                .WithMessage("Search term must be 100 characters or fewer.");

            RuleFor(query => query.SortBy)
                .NotEmpty().WithMessage("SortBy is required.")
                .Must(sortBy => AllowedSortFields.Contains(sortBy))
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");
        }
    }
}
