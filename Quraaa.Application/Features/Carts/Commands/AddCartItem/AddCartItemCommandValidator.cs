using FluentValidation;

namespace Quraaa.Application.Features.Carts.Commands.AddCartItem
{
    public class AddCartItemCommandValidator : AbstractValidator<AddCartItemCommand>
    {
        public AddCartItemCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.ListingId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }
}
