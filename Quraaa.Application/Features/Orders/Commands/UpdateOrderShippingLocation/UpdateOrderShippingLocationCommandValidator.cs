using FluentValidation;

namespace Quraaa.Application.Features.Orders.Commands.UpdateOrderShippingLocation
{
    public class UpdateOrderShippingLocationCommandValidator : AbstractValidator<UpdateOrderShippingLocationCommand>
    {
        public UpdateOrderShippingLocationCommandValidator()
        {
            RuleFor(x => x.BuyerUserId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        }
    }
}
