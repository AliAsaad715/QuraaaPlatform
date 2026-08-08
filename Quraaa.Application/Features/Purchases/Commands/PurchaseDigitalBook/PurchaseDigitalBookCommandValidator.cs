using FluentValidation;

namespace Quraaa.Application.Features.Purchases.Commands.PurchaseDigitalBook
{
    public sealed class PurchaseDigitalBookCommandValidator
        : AbstractValidator<PurchaseDigitalBookCommand>
    {
        public PurchaseDigitalBookCommandValidator()
        {
            RuleFor(x => x.RequestingUserId)
                .NotEmpty().WithMessage("Requesting user ID is required.");

            RuleFor(x => x.ListingId)
                .NotEmpty().WithMessage("Listing ID is required.");
        }
    }
}
