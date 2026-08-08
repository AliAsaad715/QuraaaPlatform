using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Orders.Interfaces;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public sealed class PaymentEventInboxRepository : IPaymentEventInbox
    {
        private readonly ApplicationDbContext _context;

        public PaymentEventInboxRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<bool> ExistsAsync(
            string provider,
            string eventId,
            CancellationToken cancellationToken = default)
        {
            return _context.Set<ProcessedPaymentEvent>()
                .AsNoTracking()
                .AnyAsync(
                    paymentEvent => paymentEvent.Provider == provider
                        && paymentEvent.EventId == eventId,
                    cancellationToken);
        }

        public async Task AddAsync(
            string provider,
            string eventId,
            string eventType,
            Guid? orderId = null,
            Guid? paymentAttemptId = null,
            CancellationToken cancellationToken = default)
        {
            var paymentEvent = new ProcessedPaymentEvent(
                provider,
                eventId,
                eventType,
                orderId,
                paymentAttemptId);

            await _context.Set<ProcessedPaymentEvent>()
                .AddAsync(paymentEvent, cancellationToken);
        }
    }
}
