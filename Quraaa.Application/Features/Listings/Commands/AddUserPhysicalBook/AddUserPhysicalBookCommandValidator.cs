using FluentValidation;

namespace Quraaa.Application.Features.Listings.Commands.AddUserPhysicalBook
{
    public sealed class AddUserPhysicalBookCommandValidator
        : AbstractValidator<AddUserPhysicalBookCommand>
    {
        public AddUserPhysicalBookCommandValidator()
        {
            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than zero.");

            RuleFor(x => x.Condition)
                .IsInEnum()
                .WithMessage("Invalid book condition value.");

            RuleFor(x => x.Isbn)
                .NotEmpty()
                .WithMessage("ISBN is required.");

            RuleFor(x => x.CoverImage)
                .NotNull()
                .WithMessage("Cover image is required for physical listings.");
        }
    }
}