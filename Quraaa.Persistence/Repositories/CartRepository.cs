using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Domain.Cart;
using Quraaa.Domain.Cart.Enums;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;

        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CartAggregate?> GetOpenCartByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<CartAggregate>()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(
                    x => x.UserId == userId
                        && (x.Status == CartStatus.Active || x.Status == CartStatus.PendingPayment)
                        && !x.IsDeleted,
                    cancellationToken);
        }

        public async Task<CartAggregate?> GetByIdAsync(
            Guid cartId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<CartAggregate>()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == cartId && !x.IsDeleted, cancellationToken);
        }

        public async Task<CartAggregate?> GetByStripeSessionIdAsync(string stripeCheckoutSessionId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<CartAggregate>()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.StripeCheckoutSessionId == stripeCheckoutSessionId && !x.IsDeleted, cancellationToken);
        }

        public async Task AddAsync(CartAggregate cart, CancellationToken cancellationToken = default)
        {
            await _context.Set<CartAggregate>().AddAsync(cart, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException(
                    "Cart changed concurrently. Reload it and retry the operation.");
            }
        }
    }
}
