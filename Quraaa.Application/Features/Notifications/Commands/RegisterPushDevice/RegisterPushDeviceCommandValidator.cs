using FluentValidation;
using Quraaa.Domain.User.Entities;

namespace Quraaa.Application.Features.Notifications.Commands.RegisterPushDevice;

public sealed class RegisterPushDeviceCommandValidator
    : AbstractValidator<RegisterPushDeviceCommand>
{
    public RegisterPushDeviceCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");

        RuleFor(command => command.DeviceToken)
            .NotEmpty()
            .WithMessage("Device token is required.")
            .MaximumLength(PushDevice.TokenMaxLength)
            .WithMessage($"Device token must not exceed {PushDevice.TokenMaxLength} characters.")
            .Must(token => token is null || !token.Any(character =>
                char.IsWhiteSpace(character) || char.IsControl(character)))
            .WithMessage("Device token cannot contain whitespace or control characters.");
    }
}
