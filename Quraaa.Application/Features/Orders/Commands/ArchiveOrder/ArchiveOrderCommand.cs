using MediatR;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Commands.ArchiveOrder
{
    public record ArchiveOrderCommand(
        Guid BuyerUserId,
        Guid OrderId)
        : IRequest<AppResult>;
}
