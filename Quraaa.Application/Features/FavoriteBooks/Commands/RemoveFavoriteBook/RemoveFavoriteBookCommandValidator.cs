using FluentValidation;

namespace Quraaa.Application.Features.FavoriteBooks.Commands.RemoveFavoriteBook
{
    public class RemoveFavoriteBookCommandValidator : AbstractValidator<RemoveFavoriteBookCommand>
    {
        public RemoveFavoriteBookCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");
        }
    }
}
