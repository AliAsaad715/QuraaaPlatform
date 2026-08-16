using FluentValidation;

namespace Quraaa.Application.Features.Listings.Commands.DeleteListings
{
    public sealed class DeleteListingsCommandValidator
        : AbstractValidator<DeleteListingsCommand>
    {
        public DeleteListingsCommandValidator()
        {
            RuleFor(x => x.RequestingUserId)
                .NotEmpty().WithMessage("User id is required.");

            RuleFor(x => x.Ids)
                .NotEmpty().WithMessage("At least one listing id is required.")
                .Must(ids => ids.Count <= 100)
                    .WithMessage("At most 100 listings can be deleted at once.")
                .Must(ids => ids.All(id => id != Guid.Empty))
                    .WithMessage("Every listing id must be a valid identifier.");
        }
    }
}
