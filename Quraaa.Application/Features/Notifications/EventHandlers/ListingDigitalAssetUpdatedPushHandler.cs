using MediatR;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Shared.Events;
using Quraaa.Domain.Marketplace.Events;
using Quraaa.Domain.Notifications;

namespace Quraaa.Application.Features.Notifications.EventHandlers
{
    /// <summary>
    /// Queues the push that tells a digital listing's buyers its file was replaced.
    /// They get one push per listing, however often the file changed in one save.
    /// </summary>
    public sealed class ListingDigitalAssetUpdatedPushHandler
        : INotificationHandler<DomainEventNotification<ListingDigitalAssetUpdatedDomainEvent>>
    {
        private readonly IListingPushNotificationRepository _notificationRepository;

        public ListingDigitalAssetUpdatedPushHandler(
            IListingPushNotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task Handle(
            DomainEventNotification<ListingDigitalAssetUpdatedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var updated = notification.DomainEvent;

            if (_notificationRepository.FindStagedDigitalAssetUpdate(updated.ListingId) is not null)
            {
                return;
            }

            await _notificationRepository.AddAsync(
                ListingPushNotification.CreateDigitalAssetUpdate(
                    updated.LibraryId,
                    updated.BookId,
                    updated.ListingId,
                    DateTime.UtcNow),
                cancellationToken);
        }
    }
}
