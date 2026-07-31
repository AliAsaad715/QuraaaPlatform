using FluentValidation;
using Quraaa.Application.Features.Payments.Common;

namespace Quraaa.Application.Features.Carts.Commands.UpdateCartItemQuantity
{
    public class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
    {
        public UpdateCartItemQuantityCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.ListingId).NotEmpty();
            RuleFor(x => x.Quantity)
                .InclusiveBetween(
                    1,
                    PaymentCheckoutLimits.MaximumQuantityPerLine);
        }
    }
}
