using FluentValidation;

namespace Quraaa.Application.Features.Listings.Commands.UpdateListing
{
    public sealed class UpdateListingCommandValidator
        : AbstractValidator<UpdateListingCommand>
    {
        public UpdateListingCommandValidator()
        {
            // At least one field must be updated
            RuleFor(x => x)
                .Must(x => x.Price.HasValue || x.Stock.HasValue || x.Condition.HasValue)
                .WithMessage("At least one of Price, Stock, or Condition must be provided.");

            When(x => x.Price.HasValue, () =>
                RuleFor(x => x.Price!.Value)
                    .GreaterThan(0)
                    .WithMessage("Price must be greater than zero."));

            When(x => x.Stock.HasValue, () =>
                RuleFor(x => x.Stock!.Value)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("Stock cannot be negative."));

            When(x => x.Condition.HasValue, () =>
                RuleFor(x => x.Condition!.Value)
                    .IsInEnum()
                    .WithMessage("Invalid book condition value."));
        }
    }
}