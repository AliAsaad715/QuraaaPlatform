using FluentValidation;

namespace Quraaa.Application.Features.Orders.Commands.ArchiveOrder
{
    public class ArchiveOrderCommandValidator : AbstractValidator<ArchiveOrderCommand>
    {
        public ArchiveOrderCommandValidator()
        {
            RuleFor(x => x.BuyerUserId).NotEmpty();
            RuleFor(x => x.OrderId).NotEmpty();
        }
    }
}
