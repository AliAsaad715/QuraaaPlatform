using Quraaa.Domain.Notifications;

namespace Quraaa.Application.Features.Notifications.Interfaces;

public interface IListingPushNotificationRepository
{
    Task<IReadOnlyList<ListingPushNotification>> ClaimReadyAsync(
        DateTime utcNow,
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ListingPushNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the publication notification for <paramref name="libraryId"/> that
    /// the current unit of work added and has not saved yet, if there is one.
    /// </summary>
    ListingPushNotification? FindStagedPublication(Guid libraryId);

    /// <summary>
    /// Returns the digital-asset-update notification for <paramref name="listingId"/>
    /// that the current unit of work added and has not saved yet, if there is one.
    /// </summary>
    ListingPushNotification? FindStagedDigitalAssetUpdate(Guid listingId);
}
