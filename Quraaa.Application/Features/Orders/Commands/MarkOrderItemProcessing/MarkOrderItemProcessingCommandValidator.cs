using FluentValidation;

namespace Quraaa.Application.Features.Orders.Commands.MarkOrderItemProcessing
{
    public sealed class MarkOrderItemProcessingCommandValidator
        : AbstractValidator<MarkOrderItemProcessingCommand>
    {
        public MarkOrderItemProcessingCommandValidator()
        {
            RuleFor(x => x.RequestingUserId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
            RuleFor(x => x.OrderItemId).NotEmpty();
        }
    }
}
