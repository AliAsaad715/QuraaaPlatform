using MediatR;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Shared.Events;
using Quraaa.Domain.Marketplace.Events;
using Quraaa.Domain.Notifications;

namespace Quraaa.Application.Features.Notifications.EventHandlers
{
    /// <summary>
    /// Queues the push that tells a library's buyers about new listings. Events saved
    /// together are handled in one unit of work, so every listing a library
    /// published in one save joins a single notification: a bulk upload sends one
    /// <c>NEW_LIBRARY_BOOK_BATCH</c> push instead of one push per book.
    /// </summary>
    public sealed class LibraryListingPublishedPushHandler
        : INotificationHandler<DomainEventNotification<LibraryListingPublishedDomainEvent>>
    {
        private readonly IListingPushNotificationRepository _notificationRepository;

        public LibraryListingPublishedPushHandler(
            IListingPushNotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task Handle(
            DomainEventNotification<LibraryListingPublishedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var published = notification.DomainEvent;

            var stagedPublication = _notificationRepository.FindStagedPublication(published.LibraryId);
            if (stagedPublication is not null)
            {
                stagedPublication.IncludePublication(published.BookId, published.ListingId);
                return;
            }

            await _notificationRepository.AddAsync(
                ListingPushNotification.CreatePublication(
                    published.LibraryId,
                    published.BookId,
                    published.ListingId,
                    itemCount: 1,
                    DateTime.UtcNow),
                cancellationToken);
        }
    }
}
