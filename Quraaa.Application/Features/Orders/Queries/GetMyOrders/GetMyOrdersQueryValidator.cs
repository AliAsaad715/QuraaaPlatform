using FluentValidation;

namespace Quraaa.Application.Features.Orders.Queries.GetMyOrders
{
    public class GetMyOrdersQueryValidator : AbstractValidator<GetMyOrdersQuery>
    {
        public GetMyOrdersQueryValidator()
        {
            RuleFor(x => x.BuyerUserId).NotEmpty();
            RuleFor(x => x.PageNumber).InclusiveBetween(1, 1_000_000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.Status!.Value)
                .IsInEnum()
                .When(x => x.Status.HasValue);
        }
    }
}
