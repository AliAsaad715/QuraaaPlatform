using FluentValidation;

namespace Quraaa.Application.Features.Reviews.Commands.UpdateBookReview
{
    public class UpdateBookReviewCommandValidator : AbstractValidator<UpdateBookReviewCommand>
    {
        public UpdateBookReviewCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty().WithMessage("User id is required.");
            RuleFor(x => x.BookId).NotEmpty().WithMessage("Book id is required.");

            RuleFor(x => x.Score)
                .InclusiveBetween(1, 5).WithMessage("Score must be between 1 and 5.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Review content is required.")
                .MaximumLength(1000).WithMessage("Review content cannot exceed 1000 characters.");
        }
    }
}
