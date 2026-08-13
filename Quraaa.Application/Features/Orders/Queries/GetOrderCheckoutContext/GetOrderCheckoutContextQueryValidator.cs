using FluentValidation;

namespace Quraaa.Application.Features.Orders.Queries.GetOrderCheckoutContext;

public sealed class GetOrderCheckoutContextQueryValidator
    : AbstractValidator<GetOrderCheckoutContextQuery>
{
    public GetOrderCheckoutContextQueryValidator()
    {
        RuleFor(query => query.BuyerUserId).NotEmpty();
    }
}
