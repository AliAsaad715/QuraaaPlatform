using FluentValidation;

namespace Quraaa.Application.Features.Purchases.Queries.GetSellHistory
{
    public sealed class GetSellHistoryQueryValidator : AbstractValidator<GetSellHistoryQuery>
    {
        public GetSellHistoryQueryValidator()
        {
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }
}