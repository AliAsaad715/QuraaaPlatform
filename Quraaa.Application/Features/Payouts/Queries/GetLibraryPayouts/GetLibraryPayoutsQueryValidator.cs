using FluentValidation;

namespace Quraaa.Application.Features.Payouts.Queries.GetLibraryPayouts
{
    public sealed class GetLibraryPayoutsQueryValidator
        : AbstractValidator<GetLibraryPayoutsQuery>
    {
        public GetLibraryPayoutsQueryValidator()
        {
            RuleFor(x => x.RequestingUserId).NotEmpty();
            RuleFor(x => x.PageNumber).InclusiveBetween(1, 1_000_000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}
