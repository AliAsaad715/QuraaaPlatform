using FluentValidation;

namespace Quraaa.Application.Features.Listings.Queries.ValidateIsbn
{
    public sealed class ValidateIsbnQueryValidator : AbstractValidator<ValidateIsbnQuery>
    {
        public ValidateIsbnQueryValidator()
        {
            RuleFor(x => x.Isbn)
                .NotEmpty()
                .WithMessage("ISBN is required.");
        }
    }
}
