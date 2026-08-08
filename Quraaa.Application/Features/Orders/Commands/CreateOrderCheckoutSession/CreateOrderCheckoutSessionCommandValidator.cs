using FluentValidation;

namespace Quraaa.Application.Features.Orders.Commands.CreateOrderCheckoutSession
{
    public class CreateOrderCheckoutSessionCommandValidator : AbstractValidator<CreateOrderCheckoutSessionCommand>
    {
        public CreateOrderCheckoutSessionCommandValidator()
        {
            RuleFor(x => x.BuyerUserId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
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
        }
    }
}
