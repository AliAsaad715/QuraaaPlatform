using FluentValidation;

namespace Quraaa.Application.Features.Listings.Commands.UpdateListingDigitalAsset
{
    public sealed class UpdateListingDigitalAssetCommandValidator : AbstractValidator<UpdateListingDigitalAssetCommand>
    {
        public UpdateListingDigitalAssetCommandValidator()
        {
            RuleFor(x => x.ListingId)
                .NotEmpty()
                .WithMessage("Listing id is required.");

            RuleFor(x => x.DigitalAsset)
                .NotNull()
                .WithMessage("Digital asset is required.")
                .Must(file => file is null || string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Digital asset must be a PDF file.");
        }
    }
}
