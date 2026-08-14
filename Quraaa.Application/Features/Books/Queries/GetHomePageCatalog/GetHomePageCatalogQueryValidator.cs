using FluentValidation;

namespace Quraaa.Application.Features.Books.Queries.GetHomePageCatalog
{
    public class GetHomePageCatalogQueryValidator : AbstractValidator<GetHomePageCatalogQuery>
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

        public GetHomePageCatalogQueryValidator()
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

            RuleFor(x => x.Format)
                .IsInEnum().When(x => x.Format.HasValue)
                .WithMessage("The provided format value is invalid.");

            RuleFor(x => x.Condition)
                .IsInEnum().When(x => x.Condition.HasValue)
                .WithMessage("The provided condition value is invalid.");

            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue)
                .WithMessage("Minimum price cannot be negative.");

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue)
                .WithMessage("Maximum price cannot be negative.");

            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
                .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
                .WithMessage("Maximum price must not be less than minimum price.");
        }
    }
}
