using FluentValidation;

namespace Quraaa.Application.Features.Reviews.Commands.DeleteBookReview
{
    public class DeleteBookReviewCommandValidator : AbstractValidator<DeleteBookReviewCommand>
    {
        public DeleteBookReviewCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User id is required.");
            RuleFor(x => x.BookId).NotEmpty().WithMessage("Book id is required.");
        }
    }
}
