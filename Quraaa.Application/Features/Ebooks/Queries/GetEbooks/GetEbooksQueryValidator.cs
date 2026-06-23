using FluentValidation;

namespace Quraaa.Application.Features.Ebooks.Queries.GetEbooks
{
    public class GetEbooksQueryValidator : AbstractValidator<GetEbooksQuery>
    {
        public GetEbooksQueryValidator()
        {
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
