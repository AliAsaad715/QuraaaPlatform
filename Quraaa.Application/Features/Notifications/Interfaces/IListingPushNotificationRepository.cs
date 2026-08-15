using Quraaa.Domain.Notifications;

namespace Quraaa.Application.Features.Notifications.Interfaces;

public interface IListingPushNotificationRepository
{
    Task<IReadOnlyList<ListingPushNotification>> ClaimReadyAsync(
        DateTime utcNow,
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
