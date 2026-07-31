using FluentValidation;

namespace Quraaa.Application.Features.Orders.Queries.GetSellerOrderItems
{
    public sealed class GetSellerOrderItemsQueryValidator
        : AbstractValidator<GetSellerOrderItemsQuery>
    {
        public GetSellerOrderItemsQueryValidator()
        {
            RuleFor(x => x.RequestingUserId).NotEmpty();
            RuleFor(x => x.PageNumber).InclusiveBetween(1, 1_000_000);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
            RuleFor(x => x.FulfillmentStatus!.Value)
                .IsInEnum()
                .When(x => x.FulfillmentStatus.HasValue);
        }
    }
}
