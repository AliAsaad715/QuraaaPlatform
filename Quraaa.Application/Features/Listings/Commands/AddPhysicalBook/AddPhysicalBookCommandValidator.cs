using FluentValidation;

namespace Quraaa.Application.Features.Listings.Commands.AddPhysicalBook
{
    public sealed class AddPhysicalBookCommandValidator
        : AbstractValidator<AddPhysicalBookCommand>
    {
        public AddPhysicalBookCommandValidator()
        {
            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than zero.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");

            RuleFor(x => x.Condition)
                .IsInEnum()
                .WithMessage("Invalid book condition value.");
        }
    }
}