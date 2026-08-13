using FluentValidation;

namespace Quraaa.Application.Features.Ratings.Commands.RateBook
{
    public class RateBookCommandValidator : AbstractValidator<RateBookCommand>
    {
        public RateBookCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");

            RuleFor(x => x.Score)
                .InclusiveBetween(1, 5).WithMessage("Score must be between 1 and 5.");
        }
    }
}
