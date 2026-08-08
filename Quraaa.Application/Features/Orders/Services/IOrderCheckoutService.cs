using Quraaa.Application.Features.Orders.Common;
using Quraaa.Domain.Cart;
using Quraaa.Domain.Orders;

namespace Quraaa.Application.Features.Orders.Services
{
    public interface IOrderCheckoutService
    {
        Task<OrderCheckoutResponse> EnsureCheckoutSessionAsync(
            OrderAggregate order,
            CartAggregate cart,
            string successUrl,
            string cancelUrl,
            CancellationToken cancellationToken = default);
    }
}
