using FluentValidation;

namespace Quraaa.Application.Features.Profiles.Commands.DeleteLocation;

public sealed class DeleteLocationCommandValidator : AbstractValidator<DeleteLocationCommand>
{
    public DeleteLocationCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.LocationId).NotEmpty();
    }
}
