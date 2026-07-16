using Quraaa.Domain.Cart;

namespace Quraaa.Application.Features.Carts.Interfaces
{
    public interface ICartRepository
    {
        Task<CartAggregate?> GetOpenCartByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<CartAggregate?> GetByStripeSessionIdAsync(string stripeCheckoutSessionId, CancellationToken cancellationToken = default);
        Task AddAsync(CartAggregate cart, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
