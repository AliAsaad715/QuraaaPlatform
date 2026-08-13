using FluentValidation;

namespace Quraaa.Application.Features.Comments.Commands.DeleteComment
{
    public class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
    {
        public DeleteCommentCommandValidator()
        {
            RuleFor(x => x.CommentId)
                .NotEmpty().WithMessage("Comment id is required.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");
        }
    }
}
