using FluentValidation;

namespace Quraaa.Application.Features.Carts.Commands.CreateCheckoutSession
{
    public class CreateCheckoutSessionCommandValidator : AbstractValidator<CreateCheckoutSessionCommand>
    {
        public CreateCheckoutSessionCommandValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.SuccessUrl).NotEmpty().Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).WithMessage("Success URL must be valid.");
            RuleFor(x => x.CancelUrl).NotEmpty().Must(url => Uri.IsWellFormedUriString(url, UriKind.Absolute)).WithMessage("Cancel URL must be valid.");
        }
    }
}
