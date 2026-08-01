using FluentValidation;

namespace Quraaa.Application.Features.Listings.Commands.AddDigitalBook
{
    public sealed class AddDigitalBookCommandValidator : AbstractValidator<AddDigitalBookCommand>
    {
        public AddDigitalBookCommandValidator()
        {
            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than zero.");

            RuleFor(x => x.Isbn)
                .NotEmpty()
                .WithMessage("ISBN is required.");

            RuleFor(x => x.DigitalAsset)
                .NotNull()
                .WithMessage("Digital asset is required.")
                .Must(file => file is null || string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Digital asset must be a PDF file.");
        }
    }
}