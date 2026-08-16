using FluentValidation;

namespace Quraaa.Application.Features.Reviews.Queries.GetUserBookReview
{
    public class GetUserBookReviewQueryValidator : AbstractValidator<GetUserBookReviewQuery>
    {
        public GetUserBookReviewQueryValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User id is required.");
            RuleFor(x => x.BookId).NotEmpty().WithMessage("Book id is required.");
        }
    }
}
