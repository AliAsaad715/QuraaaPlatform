using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Notifications.Common;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Features.Purchases.Interfaces;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.Notifications.Enums;

namespace Quraaa.Application.Features.Notifications.Commands.DispatchListingPushNotifications;

public sealed class DispatchListingPushNotificationsCommandHandler
    : IRequestHandler<
        DispatchListingPushNotificationsCommand,
        DispatchListingPushNotificationsResult>
{
    private const int MaximumDeliveryAttempts = 6;
    private static readonly TimeSpan DeliveryLeaseDuration = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(30),
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(12)
    ];

    private readonly IListingPushNotificationRepository _notificationRepository;
    private readonly IBookPurchaseRepository _bookPurchaseRepository;
    private readonly IPushDeviceRepository _pushDeviceRepository;
    private readonly IBookRepository _bookRepository;
    private readonly ILibraryRepository _libraryRepository;
    private readonly IFirebaseNotificationService _firebaseNotificationService;
    private readonly ILogger<DispatchListingPushNotificationsCommandHandler> _logger;

    public DispatchListingPushNotificationsCommandHandler(
        IListingPushNotificationRepository notificationRepository,
        IBookPurchaseRepository bookPurchaseRepository,
        IPushDeviceRepository pushDeviceRepository,
        IBookRepository bookRepository,
        ILibraryRepository libraryRepository,
        IFirebaseNotificationService firebaseNotificationService,
        ILogger<DispatchListingPushNotificationsCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _bookPurchaseRepository = bookPurchaseRepository;
        _pushDeviceRepository = pushDeviceRepository;
        _bookRepository = bookRepository;
        _libraryRepository = libraryRepository;
        _firebaseNotificationService = firebaseNotificationService;
        _logger = logger;
    }

    public async Task<DispatchListingPushNotificationsResult> Handle(
        DispatchListingPushNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BatchSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.BatchSize),
                "The listing notification batch size must be between 1 and 100.");
        }

        var claimedCount = 0;
        var completedCount = 0;
        var retryScheduledCount = 0;
        var abandonedCount = 0;

        for (var index = 0; index < request.BatchSize; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var claimedNotifications = await _notificationRepository.ClaimReadyAsync(
                DateTime.UtcNow,
                1,
                DeliveryLeaseDuration,
                cancellationToken);
            if (claimedNotifications.Count == 0)
            {
                break;
            }

            var notification = claimedNotifications[0];
            claimedCount++;

            var previousState = notification.State;
            var invalidTokens = await DeliverAsync(notification, cancellationToken);

            await _notificationRepository.SaveChangesAsync(CancellationToken.None);
            await RemoveInvalidTokensAsync(notification, invalidTokens);

            completedCount += notification.State == NotificationDeliveryState.Sent ? 1 : 0;
            retryScheduledCount += previousState == NotificationDeliveryState.Pending
                && notification.State == NotificationDeliveryState.Pending
                    ? 1
                    : 0;
            abandonedCount += notification.State == NotificationDeliveryState.Abandoned ? 1 : 0;

            notification.ReleaseLease();
            await _notificationRepository.SaveChangesAsync(CancellationToken.None);
        }

        return new DispatchListingPushNotificationsResult(
            claimedCount,
            completedCount,
            retryScheduledCount,
            abandonedCount);
    }

    private async Task<IReadOnlyCollection<string>> DeliverAsync(
        ListingPushNotification notification,
        CancellationToken cancellationToken)
    {
        var message = await BuildMessageAsync(notification, cancellationToken);
        if (message is null)
        {
            notification.MarkAbandoned(DateTime.UtcNow);
            return [];
        }

        var buyerUserIds = notification.Type switch
        {
            ListingPushNotificationType.LibraryListingsPublished =>
                await _bookPurchaseRepository.GetDistinctBuyerUserIdsByLibraryAsync(
                    notification.LibraryId,
                    cancellationToken),
            ListingPushNotificationType.ListingDigitalAssetUpdated =>
                await _bookPurchaseRepository.GetDistinctBuyerUserIdsByListingAsync(
                    notification.ListingId!.Value,
                    cancellationToken),
            _ => throw new InvalidOperationException(
                $"Unsupported listing notification type: {notification.Type}.")
        };

        if (buyerUserIds.Count == 0)
        {
            notification.MarkCompleted(DateTime.UtcNow);
            return [];
        }

        var registeredTokens = await _pushDeviceRepository.GetTokensByUserIdsAsync(
            buyerUserIds,
            cancellationToken);
        var pendingTokens = registeredTokens
            .Where(token => !notification.WasDeliveredTo(token))
            .ToArray();

        if (pendingTokens.Length == 0)
        {
            notification.MarkCompleted(DateTime.UtcNow);
            return [];
        }

        FirebaseMulticastResult result;
        try
        {
            result = await _firebaseNotificationService.SendMulticastAsync(
                pendingTokens,
                message.Title,
                message.Body,
                message.Data,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Listing push delivery failed for outbox notification {NotificationId}.",
                notification.Id);

            notification.RecordPushAttempt(
                [],
                true,
                DateTime.UtcNow,
                GetNextAttemptAtUtc(notification.AttemptCount),
                MaximumDeliveryAttempts);
            return [];
        }

        notification.RecordPushAttempt(
            result.SuccessfulTokens,
            result.HasTransientFailures,
            DateTime.UtcNow,
            GetNextAttemptAtUtc(notification.AttemptCount),
            MaximumDeliveryAttempts);

        return result.InvalidTokens;
    }

    private async Task<PushMessage?> BuildMessageAsync(
        ListingPushNotification notification,
        CancellationToken cancellationToken)
    {
        var library = await _libraryRepository.GetByIdAsync(
            notification.LibraryId,
            cancellationToken);
        if (library is null)
        {
            _logger.LogError(
                "Library {LibraryId} was not found for listing notification {NotificationId}.",
                notification.LibraryId,
                notification.Id);
            return null;
        }

        if (notification.IsBatchPublication)
        {
            return new PushMessage(
                $"New arrivals from {library.LibraryName}!",
                $"{library.LibraryName} just published {notification.ItemCount} new books. Tap to explore!",
                new Dictionary<string, string>
                {
                    ["type"] = "NEW_LIBRARY_BOOK_BATCH",
                    ["libraryId"] = notification.LibraryId.ToString("D"),
                    ["bookCount"] = notification.ItemCount.ToString()
                });
        }

        var book = await _bookRepository.GetByIdAsync(
            notification.BookId!.Value,
            cancellationToken);
        if (book is null)
        {
            _logger.LogError(
                "Book {BookId} was not found for listing notification {NotificationId}.",
                notification.BookId,
                notification.Id);
            return null;
        }

        return notification.Type switch
        {
            ListingPushNotificationType.LibraryListingsPublished =>
                new PushMessage(
                    $"New Arrival from {library.LibraryName}!",
                    $"\"{library.LibraryName}\" just published a new book: \"{book.Title}\". Tap to explore!",
                    new Dictionary<string, string>
                    {
                        ["type"] = "NEW_LIBRARY_BOOK",
                        ["bookId"] = notification.BookId.Value.ToString("D"),
                        ["listingId"] = notification.ListingId!.Value.ToString("D"),
                        ["libraryId"] = notification.LibraryId.ToString("D")
                    }),
            ListingPushNotificationType.ListingDigitalAssetUpdated =>
                new PushMessage(
                    "New Version Available!",
                    $"The library \"{library.LibraryName}\" has published an updated version of \"{book.Title}\". Check out the latest content in your library!",
                    new Dictionary<string, string>
                    {
                        ["type"] = "LISTING_UPDATED",
                        ["bookId"] = notification.BookId.Value.ToString("D"),
                        ["listingId"] = notification.ListingId!.Value.ToString("D")
                    }),
            _ => throw new InvalidOperationException(
                $"Unsupported listing notification type: {notification.Type}.")
        };
    }

    private async Task RemoveInvalidTokensAsync(
        ListingPushNotification notification,
        IReadOnlyCollection<string> invalidTokens)
    {
        if (invalidTokens.Count == 0)
        {
            return;
        }

        try
        {
            await _pushDeviceRepository.RemoveTokensAsync(
                invalidTokens,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid FCM tokens could not be removed for listing notification {NotificationId}; the delivery outcome remains recorded.",
                notification.Id);
        }
    }

    private static DateTime GetNextAttemptAtUtc(int completedAttemptCount)
    {
        var delayIndex = Math.Min(completedAttemptCount, RetryDelays.Length - 1);
        return DateTime.UtcNow.Add(RetryDelays[delayIndex]);
    }

    private sealed record PushMessage(
        string Title,
        string Body,
        IReadOnlyDictionary<string, string> Data);
}
