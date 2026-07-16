using FluentValidation;

namespace Quraaa.Application.Features.Carts.Commands.UpdateCartItemQuantity
{
    public class UpdateCartItemQuantityCommandValidator : AbstractValidator<UpdateCartItemQuantityCommand>
    {
        public UpdateCartItemQuantityCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.ListingId).NotEmpty();
            RuleFor(x => x.Quantity).GreaterThan(0);
        }
    }
}
