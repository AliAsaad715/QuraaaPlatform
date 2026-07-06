using FluentValidation;

namespace Quraaa.Application.Features.FavoriteBooks.Commands.AddFavoriteBook
{
    public class AddFavoriteBookCommandValidator : AbstractValidator<AddFavoriteBookCommand>
    {
        public AddFavoriteBookCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");
        }
    }
}
