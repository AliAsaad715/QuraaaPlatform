using FluentValidation;

namespace Quraaa.Application.Features.Purchases.Queries.GetBuyHistory
{
    public sealed class GetBuyHistoryQueryValidator : AbstractValidator<GetBuyHistoryQuery>
    {
        public GetBuyHistoryQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}