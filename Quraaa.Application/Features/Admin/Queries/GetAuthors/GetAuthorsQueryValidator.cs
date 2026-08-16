using FluentValidation;

namespace Quraaa.Application.Features.Admin.Queries.GetAuthors
{
    public sealed class GetAuthorsQueryValidator : AbstractValidator<GetAuthorsQuery>
    {
        public GetAuthorsQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

            RuleFor(x => x.SearchTerm)
                .MaximumLength(200)
                .WithMessage("The search term cannot exceed 200 characters.");
        }
    }
}
