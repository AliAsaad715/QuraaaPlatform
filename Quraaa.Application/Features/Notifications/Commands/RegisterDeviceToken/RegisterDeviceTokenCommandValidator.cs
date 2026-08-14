using FluentValidation;

namespace Quraaa.Application.Features.Notifications.Commands.RegisterDeviceToken
{
    public class RegisterDeviceTokenCommandValidator : AbstractValidator<RegisterDeviceTokenCommand>
    {
        public RegisterDeviceTokenCommandValidator()
        {
            RuleFor(x => x.DeviceToken)
                .NotEmpty().WithMessage("Device token is required.")
                .MaximumLength(4096).WithMessage("Device token is too long.");
        }
    }
}
