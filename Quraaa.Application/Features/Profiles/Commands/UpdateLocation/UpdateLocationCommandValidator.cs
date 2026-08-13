using FluentValidation;
using Quraaa.Domain.User.Entities;

namespace Quraaa.Application.Features.Profiles.Commands.UpdateLocation;

public sealed class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.LocationId).NotEmpty();
        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Location name is required.")
            .Must(name => name is null || name.Trim().Length <= UserLocation.NameMaxLength)
            .WithMessage($"Location name cannot exceed {UserLocation.NameMaxLength} characters.");
        RuleFor(command => command.Address)
            .Must(address =>
                string.IsNullOrWhiteSpace(address)
                || address.Trim().Length <= UserLocation.AddressMaxLength)
            .WithMessage($"Location address cannot exceed {UserLocation.AddressMaxLength} characters.");
        RuleFor(command => command.Latitude)
            .Cascade(CascadeMode.Stop)
            .Must(double.IsFinite)
            .WithMessage("Latitude must be a finite number.")
            .InclusiveBetween(-90, 90);
        RuleFor(command => command.Longitude)
            .Cascade(CascadeMode.Stop)
            .Must(double.IsFinite)
            .WithMessage("Longitude must be a finite number.")
            .InclusiveBetween(-180, 180);
    }
}
