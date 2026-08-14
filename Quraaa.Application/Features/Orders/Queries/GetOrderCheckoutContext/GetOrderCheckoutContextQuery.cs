using MediatR;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Shared.Results;

namespace Quraaa.Application.Features.Orders.Queries.GetOrderCheckoutContext;

public sealed record GetOrderCheckoutContextQuery(Guid BuyerUserId)
    : IRequest<AppResult<OrderCheckoutContextResponse>>;
