using MediatR;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Application.Shared.Events;
using Quraaa.Domain.Marketplace.Events;

namespace Quraaa.Application.Features.Notifications.EventHandlers
{
    public sealed class LibraryListingPublishedDomainEventHandler
        : INotificationHandler<DomainEventNotification<LibraryListingPublishedDomainEvent>>
    {
        private readonly IBookPurchaseRepository _bookPurchaseRepository;
        private readonly IUserDeviceTokenRepository _userDeviceTokenRepository;
        private readonly IBookRepository _bookRepository;
        private readonly ILibraryRepository _libraryRepository;
        private readonly IFirebaseNotificationService _firebaseNotificationService;

        public LibraryListingPublishedDomainEventHandler(
            IBookPurchaseRepository bookPurchaseRepository,
            IUserDeviceTokenRepository userDeviceTokenRepository,
            IBookRepository bookRepository,
            ILibraryRepository libraryRepository,
            IFirebaseNotificationService firebaseNotificationService)
        {
            _bookPurchaseRepository = bookPurchaseRepository;
            _userDeviceTokenRepository = userDeviceTokenRepository;
            _bookRepository = bookRepository;
            _libraryRepository = libraryRepository;
            _firebaseNotificationService = firebaseNotificationService;
        }

        public async Task Handle(
            DomainEventNotification<LibraryListingPublishedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var domainEvent = notification.DomainEvent;

            var buyerUserIds = await _bookPurchaseRepository.GetDistinctBuyerUserIdsByLibraryAsync(
                domainEvent.LibraryId, cancellationToken);
            if (buyerUserIds.Count == 0)
            {
                return;
            }

            var deviceTokens = await _userDeviceTokenRepository.GetTokensByUserIdsAsync(
                buyerUserIds, cancellationToken);
            if (deviceTokens.Count == 0)
            {
                return;
            }

            var book = await _bookRepository.GetByIdAsync(domainEvent.BookId, cancellationToken);
            var library = await _libraryRepository.GetByIdAsync(domainEvent.LibraryId, cancellationToken);
            if (book is null || library is null)
            {
                return;
            }

            var data = new Dictionary<string, string>
            {
                ["type"] = "NEW_LIBRARY_BOOK",
                ["bookId"] = domainEvent.BookId.ToString(),
                ["libraryId"] = domainEvent.LibraryId.ToString()
            };

            var result = await _firebaseNotificationService.SendMulticastAsync(
                deviceTokens,
                $"New Arrival from {library.LibraryName}!",
                $"\"{library.LibraryName}\" just published a new book: \"{book.Title}\". Tap to explore!",
                data,
                cancellationToken);

            if (result.InvalidTokens.Count > 0)
            {
                await _userDeviceTokenRepository.RemoveTokensAsync(result.InvalidTokens, cancellationToken);
            }
        }
    }
}
