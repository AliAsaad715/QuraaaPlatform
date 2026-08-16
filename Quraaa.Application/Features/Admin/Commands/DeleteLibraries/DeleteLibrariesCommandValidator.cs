using FluentValidation;

namespace Quraaa.Application.Features.Admin.Commands.DeleteLibraries
{
    public sealed class DeleteLibrariesCommandValidator
        : AbstractValidator<DeleteLibrariesCommand>
    {
        public DeleteLibrariesCommandValidator()
        {
            RuleFor(x => x.AdminId)
                .NotEmpty().WithMessage("Admin id is required.");

            RuleFor(x => x.Ids)
                .NotEmpty().WithMessage("At least one id is required.")
                .Must(ids => ids.Count <= 100)
                    .WithMessage("At most 100 records can be deleted at once.")
                .Must(ids => ids.All(id => id != Guid.Empty))
                    .WithMessage("Every id must be a valid identifier.");
        }
    }
}
