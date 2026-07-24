using FluentValidation;

namespace Quraaa.Application.Features.Profiles.Commands.CreateLocation
{
    public class UpsertLocationCommandValidator : AbstractValidator<UpsertLocationCommand>
    {
        public UpsertLocationCommandValidator()
        {
            RuleFor(x => x.Latitude).NotEmpty().InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).NotEmpty().InclusiveBetween(-180, 180);
        }
    }
}
