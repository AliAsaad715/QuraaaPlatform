using FluentValidation;

namespace Quraaa.Application.Features.Orders.Commands.MarkOrderItemFulfilled
{
    public sealed class MarkOrderItemFulfilledCommandValidator
        : AbstractValidator<MarkOrderItemFulfilledCommand>
    {
        public MarkOrderItemFulfilledCommandValidator()
        {
            RuleFor(x => x.RequestingUserId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.OrderItemId).NotEmpty();
        }
    }
}
