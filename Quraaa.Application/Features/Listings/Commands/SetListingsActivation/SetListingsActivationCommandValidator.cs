using FluentValidation;

namespace Quraaa.Application.Features.Listings.Commands.SetListingsActivation
{
    public sealed class SetListingsActivationCommandValidator
        : AbstractValidator<SetListingsActivationCommand>
    {
        public SetListingsActivationCommandValidator()
        {
            RuleFor(x => x.RequestingUserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.Ids)
                .NotEmpty().WithMessage("At least one listing id is required.")
                .Must(ids => ids.Count <= 200)
                    .WithMessage("At most 200 listings can be updated at once.")
                .Must(ids => ids.All(id => id != Guid.Empty))
                    .WithMessage("Every listing id must be a valid identifier.");
        }
    }
}
