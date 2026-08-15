using FluentValidation;

namespace Quraaa.Application.Features.Books.Queries.GetBookVersions
{
    public sealed class GetBookVersionsQueryValidator : AbstractValidator<GetBookVersionsQuery>
    {
        public GetBookVersionsQueryValidator()
        {
            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");
        }
    }
}
