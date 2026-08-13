using FluentValidation;

namespace Quraaa.Application.Features.Comments.Commands.UpdateComment
{
    public class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
    {
        public UpdateCommentCommandValidator()
        {
            RuleFor(x => x.CommentId)
                .NotEmpty().WithMessage("Comment id is required.");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.BookId)
                .NotEmpty().WithMessage("Book id is required.");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Comment content is required.")
                .MaximumLength(2000).WithMessage("Comment content cannot exceed 2000 characters.");
        }
    }
}
