using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Domain.Payouts;

namespace Quraaa.Application.Features.Payouts.Interfaces
{
    public interface ISellerPayoutRepository
    {
        Task AddRangeAsync(
            IEnumerable<SellerPayoutAggregate> payouts,
            CancellationToken cancellationToken = default);

        Task<SellerPayoutAggregate?> GetByIdAsync(
            Guid payoutId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ids of pending payouts whose next attempt is due, ordered oldest
        /// scheduled first.
        /// </summary>
        Task<IReadOnlyCollection<Guid>> GetDuePayoutIdsAsync(
            DateTime dueOnOrBeforeUtc,
            int take,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyCollection<SellerPayoutResponse> Items, int TotalCount)> GetPagedForLibraryAsync(
            Guid libraryId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Moves every pending payout for the library to the given next-attempt
        /// time so a freshly configured wallet is paid without waiting for the
        /// no-wallet backoff. Returns the number of rescheduled payouts.
        /// </summary>
        Task<int> ReschedulePendingForLibraryAsync(
            Guid libraryId,
            DateTime nextAttemptAtUtc,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
