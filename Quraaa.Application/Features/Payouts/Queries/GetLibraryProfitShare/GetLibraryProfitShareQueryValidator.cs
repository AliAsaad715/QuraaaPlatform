using FluentValidation;

namespace Quraaa.Application.Features.Payouts.Queries.GetLibraryProfitShare
{
    public sealed class GetLibraryProfitShareQueryValidator
        : AbstractValidator<GetLibraryProfitShareQuery>
    {
        public GetLibraryProfitShareQueryValidator()
        {
            RuleFor(x => x.LibraryId)
                .NotEmpty()
                .WithMessage("Library id is required.");
        }
    }
}
