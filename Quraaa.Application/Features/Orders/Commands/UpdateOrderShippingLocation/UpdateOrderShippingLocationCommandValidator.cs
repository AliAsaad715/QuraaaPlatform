using FluentValidation;

namespace Quraaa.Application.Features.Orders.Commands.UpdateOrderShippingLocation
{
    public class UpdateOrderShippingLocationCommandValidator : AbstractValidator<UpdateOrderShippingLocationCommand>
    {
        public UpdateOrderShippingLocationCommandValidator()
        {
            RuleFor(x => x.BuyerUserId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x)
                .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
                .WithMessage("Shipping latitude and longitude must be provided together.");
            RuleFor(x => x)
                .Must(x => x.ShippingLocationId.HasValue != x.Latitude.HasValue)
                .WithMessage("Provide exactly one saved shipping location id or shipping coordinate pair.");
            RuleFor(x => x.ShippingLocationId!.Value)
                .NotEmpty()
                .When(x => x.ShippingLocationId.HasValue);
            RuleFor(x => x.Latitude!.Value)
                .Cascade(CascadeMode.Stop)
                .Must(double.IsFinite)
                .WithMessage("Shipping latitude must be a finite number.")
                .InclusiveBetween(-90, 90)
                .When(x => x.Latitude.HasValue);
            RuleFor(x => x.Longitude!.Value)
                .Cascade(CascadeMode.Stop)
                .Must(double.IsFinite)
                .WithMessage("Shipping longitude must be a finite number.")
                .InclusiveBetween(-180, 180)
                .When(x => x.Longitude.HasValue);
        }
    }
}
