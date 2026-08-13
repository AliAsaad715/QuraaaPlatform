using FluentValidation;

namespace Quraaa.Application.Features.Profiles.Commands.SetDefaultLocation;

public sealed class SetDefaultLocationCommandValidator : AbstractValidator<SetDefaultLocationCommand>
{
    public SetDefaultLocationCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.LocationId).NotEmpty();
    }
}
