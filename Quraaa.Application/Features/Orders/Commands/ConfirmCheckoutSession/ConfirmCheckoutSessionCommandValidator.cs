using FluentValidation;

namespace Quraaa.Application.Features.Orders.Commands.ConfirmCheckoutSession
{
    public sealed class ConfirmCheckoutSessionCommandValidator
        : AbstractValidator<ConfirmCheckoutSessionCommand>
    {
        public ConfirmCheckoutSessionCommandValidator()
        {
            RuleFor(x => x.BuyerUserId)
                .NotEmpty().WithMessage("Buyer id is required.");

            RuleFor(x => x.SessionId)
                .NotEmpty().WithMessage("The checkout session id is required.")
                .MaximumLength(255);
        }
    }
}
