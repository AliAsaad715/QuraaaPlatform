using FluentValidation;

namespace Quraaa.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.BuyerUserId).NotEmpty();
            RuleFor(x => x.SuccessUrl)
                .NotEmpty()
                .MaximumLength(2048)
                .Must(OrderUrlValidator.IsAllowedRedirectUrl)
                .WithMessage("Success URL must be an absolute HTTP or HTTPS URL.");
            RuleFor(x => x.CancelUrl)
                .NotEmpty()
                .MaximumLength(2048)
                .Must(OrderUrlValidator.IsAllowedRedirectUrl)
                .WithMessage("Cancel URL must be an absolute HTTP or HTTPS URL.");
            RuleFor(x => x)
                .Must(x => x.ShippingLatitude.HasValue == x.ShippingLongitude.HasValue)
                .WithMessage("Shipping latitude and longitude must be provided together.");
            RuleFor(x => x)
                .Must(x => !x.ShippingLocationId.HasValue
                    || (!x.ShippingLatitude.HasValue && !x.ShippingLongitude.HasValue))
                .WithMessage("Provide either a saved shipping location id or shipping coordinates, not both.");
            RuleFor(x => x.ShippingLocationId!.Value)
                .NotEmpty()
                .When(x => x.ShippingLocationId.HasValue);
            RuleFor(x => x.ShippingLatitude!.Value)
                .Cascade(CascadeMode.Stop)
                .Must(double.IsFinite)
                .WithMessage("Shipping latitude must be a finite number.")
                .InclusiveBetween(-90, 90)
                .When(x => x.ShippingLatitude.HasValue);
            RuleFor(x => x.ShippingLongitude!.Value)
                .Cascade(CascadeMode.Stop)
                .Must(double.IsFinite)
                .WithMessage("Shipping longitude must be a finite number.")
                .InclusiveBetween(-180, 180)
                .When(x => x.ShippingLongitude.HasValue);
        }
    }
}
