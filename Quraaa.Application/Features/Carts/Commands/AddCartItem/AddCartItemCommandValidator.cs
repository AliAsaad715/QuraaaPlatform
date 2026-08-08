using FluentValidation;
using Quraaa.Application.Features.Payments.Common;

namespace Quraaa.Application.Features.Carts.Commands.AddCartItem
{
    public class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
    {
        public AddCartItemCommandValidator()
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
