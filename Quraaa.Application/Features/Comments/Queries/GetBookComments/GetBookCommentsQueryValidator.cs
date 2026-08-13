using FluentValidation;

namespace Quraaa.Application.Features.Comments.Queries.GetBookComments
{
    public class GetBookCommentsQueryValidator : AbstractValidator<GetBookCommentsQuery>
    {
        public GetBookCommentsQueryValidator()
        {
            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");

            RuleFor(x => x.PageNumber)
                .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        }
    }
}
