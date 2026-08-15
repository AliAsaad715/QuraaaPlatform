using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Domain.Payouts;
using Quraaa.Domain.Payouts.Enums;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories
{
    public class SellerPayoutRepository : ISellerPayoutRepository
    {
        private readonly ApplicationDbContext _context;

        public SellerPayoutRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(
            IEnumerable<SellerPayoutAggregate> payouts,
            CancellationToken cancellationToken = default)
        {
            await _context.SellerPayouts.AddRangeAsync(payouts, cancellationToken);
        }

        public async Task<SellerPayoutAggregate?> GetByIdAsync(
            Guid payoutId,
            CancellationToken cancellationToken = default)
        {
            return await _context.SellerPayouts
                .FirstOrDefaultAsync(
                    payout => payout.Id == payoutId,
                    cancellationToken);
        }

        public async Task<IReadOnlyCollection<Guid>> GetDuePayoutIdsAsync(
            DateTime dueOnOrBeforeUtc,
            int take,
            CancellationToken cancellationToken = default)
        {
            return await _context.SellerPayouts
                .AsNoTracking()
                .Where(payout =>
                    payout.Status == SellerPayoutStatus.Pending
                    && payout.NextAttemptAtUtc <= dueOnOrBeforeUtc)
                .OrderBy(payout => payout.NextAttemptAtUtc)
                .ThenBy(payout => payout.Id)
                .Take(take)
                .Select(payout => payout.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<(IReadOnlyCollection<SellerPayoutResponse> Items, int TotalCount)> GetPagedForLibraryAsync(
            Guid libraryId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.SellerPayouts
                .AsNoTracking()
                .Where(payout => payout.LibraryId == libraryId);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Join(
                    _context.Orders,
                    payout => payout.OrderId,
                    order => order.Id,
                    (payout, order) => new { payout, order.OrderNumber })
                .OrderByDescending(row => row.payout.CreationTime)
                .ThenBy(row => row.payout.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(row => new SellerPayoutResponse(
                    row.payout.Id,
                    row.payout.OrderId,
                    row.OrderNumber,
                    row.payout.Currency,
                    row.payout.GrossAmountMinor / 100m,
                    row.payout.ProfitSharePercent,
                    row.payout.PlatformFeeMinor / 100m,
                    row.payout.NetAmountMinor / 100m,
                    row.payout.Status,
                    row.payout.AttemptCount,
                    row.payout.PaidAtUtc,
                    row.payout.StripeTransferId,
                    row.payout.FailureReason,
                    row.payout.CreationTime))
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        public async Task<int> ReschedulePendingForLibraryAsync(
            Guid libraryId,
            DateTime nextAttemptAtUtc,
            CancellationToken cancellationToken = default)
        {
            // AttemptCount == 0 keeps this to payouts that no transfer attempt
            // has ever been rejected for (wallet parks and funds deferrals), so
            // rows in rejection backoff keep their schedule and their remaining
            // attempts. The LastAttemptAtUtc cut-off skips
            // rows a worker is holding right now (ClaimForProcessing leaves the
            // status Pending and the attempt count untouched), so a wallet
            // change cannot pull a payout back into the scan mid-transfer.
            var leaseCutoffUtc = nextAttemptAtUtc.Subtract(SellerPayoutAggregate.ProcessingLease);

            return await _context.SellerPayouts
                .Where(payout =>
                    payout.LibraryId == libraryId
                    && payout.Status == SellerPayoutStatus.Pending
                    && payout.AttemptCount == 0
                    && (payout.LastAttemptAtUtc == null
                        || payout.LastAttemptAtUtc < leaseCutoffUtc)
                    && payout.NextAttemptAtUtc > nextAttemptAtUtc)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            payout => payout.NextAttemptAtUtc,
                            nextAttemptAtUtc)
                        // Rotate the concurrency token so a worker holding a
                        // stale copy of one of these rows still conflicts.
                        .SetProperty(
                            payout => payout.LastModificationTime,
                            DateTime.UtcNow),
                    cancellationToken);
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
                    "The payout changed concurrently. Retry the operation.");
            }
        }
    }
}
