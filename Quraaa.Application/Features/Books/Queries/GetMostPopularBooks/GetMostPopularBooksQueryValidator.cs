using FluentValidation;

namespace Quraaa.Application.Features.Books.Queries.GetMostPopularBooks
{
    public class GetMostPopularBooksQueryValidator : AbstractValidator<GetMostPopularBooksQuery>
    {
        private static readonly IReadOnlySet<string> AllowedSortFields =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "popular",
                "purchases",
                "ratings",
                "averageRating"
            };

        public GetMostPopularBooksQueryValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

            RuleFor(x => x.SearchTerm)
                .MaximumLength(100).When(x => x.SearchTerm is not null)
                .WithMessage("Search term must be 100 characters or fewer.");

            RuleFor(x => x.SortBy)
                .NotEmpty().WithMessage("SortBy is required.")
                .Must(sortBy => AllowedSortFields.Contains(sortBy))
                .WithMessage($"SortBy must be one of: {string.Join(", ", AllowedSortFields)}.");
        }
    }
}
