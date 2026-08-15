using Quraaa.Domain.Notifications;

namespace Quraaa.Application.Features.BookReports.Interfaces
{
    public interface IBookModerationNotificationRepository
    {
        Task AddRangeAsync(
            IReadOnlyCollection<BookModerationNotification> notifications,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<BookModerationNotification>> ClaimReadyAsync(
            DateTime utcNow,
            int batchSize,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
