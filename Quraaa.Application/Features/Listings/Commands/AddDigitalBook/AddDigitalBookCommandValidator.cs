using FluentValidation;
using Quraaa.Application.Shared.Files;

namespace Quraaa.Application.Features.Listings.Commands.AddDigitalBook
{
    public sealed class AddDigitalBookCommandValidator : AbstractValidator<AddDigitalBookCommand>
    {
        private const long MaxPdfBytes = 100L * 1024 * 1024;

        private static readonly IReadOnlySet<string> AllowedPdfContentTypes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "application/pdf",
                "application/octet-stream"
            };

        public AddDigitalBookCommandValidator()
        {
            RuleFor(x => x.Price)
                .GreaterThan(0)
                .WithMessage("Price must be greater than zero.");

            RuleFor(x => x.Isbn)
                .NotEmpty()
                .WithMessage("ISBN is required.");

            RuleFor(x => x.DigitalAsset)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("Digital asset is required.")
                .Must(file => file!.Length > 0 && file.Length <= MaxPdfBytes)
                .WithMessage("Digital asset must be between 1 byte and 100 MB.")
                .Must(file => file is null || string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Digital asset must be a PDF file.")
                .Must(file => file is not null && AllowedPdfContentTypes.Contains(file.ContentType))
                .WithMessage("Digital asset content type must be application/pdf.")
                .Must(DocumentFileSignature.MatchesDeclaredExtension)
                .WithMessage("Digital asset content does not match the PDF file type.");
        }
    }
}
