using FluentValidation;

namespace Quraaa.Application.Features.Admin.Commands.SetUserActivation
{
    public sealed class SetUserActivationCommandValidator
        : AbstractValidator<SetUserActivationCommand>
    {
        public SetUserActivationCommandValidator()
        {
            RuleFor(x => x.AdminId)
                .NotEmpty().WithMessage("Admin id is required.");

            RuleFor(x => x.Ids)
                .NotEmpty().WithMessage("At least one id is required.")
                .Must(ids => ids.Count <= 200)
                    .WithMessage("At most 200 records can be updated at once.")
                .Must(ids => ids.All(id => id != Guid.Empty))
                    .WithMessage("Every id must be a valid identifier.");
        }
    }
}
