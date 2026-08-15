using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Application.Features.Notifications.Common;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.Notifications.Enums;

namespace Quraaa.Application.Features.BookReports.Commands.DispatchBookModerationNotifications;

/// <summary>
/// Drains the book moderation notification outbox. Mirrors the library approval
/// dispatcher: claim a lease, push, record the outcome, retry with backoff.
/// </summary>
public sealed class DispatchBookModerationNotificationsCommandHandler
    : IRequestHandler<DispatchBookModerationNotificationsCommand, DispatchBookModerationNotificationsResult>
{
    private const int MaximumDeliveryAttempts = 6;

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(60),
        TimeSpan.FromHours(6),
    ];

    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    private readonly IBookModerationNotificationRepository _notificationRepository;
    private readonly IPushDeviceRepository _pushDeviceRepository;
    private readonly IFirebaseNotificationService _firebaseNotificationService;
    private readonly ILogger<DispatchBookModerationNotificationsCommandHandler> _logger;

    public DispatchBookModerationNotificationsCommandHandler(
        IBookModerationNotificationRepository notificationRepository,
        IPushDeviceRepository pushDeviceRepository,
        IFirebaseNotificationService firebaseNotificationService,
        ILogger<DispatchBookModerationNotificationsCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _pushDeviceRepository = pushDeviceRepository;
        _firebaseNotificationService = firebaseNotificationService;
        _logger = logger;
    }

    public async Task<DispatchBookModerationNotificationsResult> Handle(
        DispatchBookModerationNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        var claimed = await _notificationRepository.ClaimReadyAsync(
            utcNow,
            request.BatchSize,
            LeaseDuration,
            cancellationToken);

        if (claimed.Count == 0)
        {
            return new DispatchBookModerationNotificationsResult(0, 0, 0, 0);
        }

        var sentCount = 0;
        var retryCount = 0;
        var abandonedCount = 0;

        foreach (var notification in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var invalidTokens = await DeliverAsync(notification, cancellationToken);

            notification.ReleaseLease();

            switch (notification.PushState)
            {
                case NotificationDeliveryState.Sent:
                    sentCount++;
                    break;
                case NotificationDeliveryState.Abandoned:
                    abandonedCount++;
                    break;
                default:
                    retryCount++;
                    break;
            }

            await _notificationRepository.SaveChangesAsync(cancellationToken);
            await RemoveInvalidPushTokensAsync(notification, invalidTokens);
        }

        return new DispatchBookModerationNotificationsResult(
            claimed.Count,
            sentCount,
            retryCount,
            abandonedCount);
    }

    private async Task<IReadOnlyCollection<string>> DeliverAsync(
        BookModerationNotification notification,
        CancellationToken cancellationToken)
    {
        PushBatchDeliveryResult result;

        try
        {
            var registeredTokens = await _pushDeviceRepository.GetTokensByUserIdAsync(
                notification.RecipientUserId,
                cancellationToken);

            var deviceTokens = registeredTokens
                .Where(token => !notification.WasDeliveredTo(token))
                .ToArray();

            if (deviceTokens.Length == 0)
            {
                // Nothing registered to deliver to. The moderation queue is the
                // authoritative record, so this notice is simply dropped rather
                // than retried forever.
                notification.MarkUndeliverable(DateTime.UtcNow);
                return [];
            }

            var (title, body) = BuildMessage(notification);

            result = await _firebaseNotificationService.SendToDevicesAsync(
                deviceTokens,
                title,
                body,
                new Dictionary<string, string>
                {
                    ["type"] = "book_moderation",
                    ["bookId"] = notification.BookId.ToString("D"),
                    ["level"] = notification.Level.ToString(),
                    ["audience"] = notification.Audience.ToString(),
                    ["reporterCount"] = notification.ReporterCount.ToString(),
                },
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
                "Book moderation push delivery failed for notification {NotificationId}.",
                notification.Id);

            notification.RecordPushAttempt(
                [],
                hasTransientFailures: true,
                DateTime.UtcNow,
                GetNextAttemptAtUtc(notification.PushAttemptCount),
                MaximumDeliveryAttempts);

            return [];
        }

        notification.RecordPushAttempt(
            result.SuccessfulDeviceTokens,
            result.HasTransientFailures,
            DateTime.UtcNow,
            GetNextAttemptAtUtc(notification.PushAttemptCount),
            MaximumDeliveryAttempts);

        return result.InvalidDeviceTokens;
    }

    private static (string Title, string Body) BuildMessage(BookModerationNotification notification)
    {
        var reporters = notification.ReporterCount == 1
            ? "1 reader"
            : $"{notification.ReporterCount} readers";

        return notification.Level switch
        {
            BookModerationLevel.Hidden => (
                "Book withheld pending review",
                $"\"{notification.BookTitle}\" was reported by {reporters} and is hidden from the catalogue while it is reviewed."),

            BookModerationLevel.Escalated when notification.Audience == BookModerationAudience.Admin => (
                "Book needs moderation",
                $"\"{notification.BookTitle}\" has now been reported by {reporters}."),

            BookModerationLevel.Escalated => (
                "A book you list was reported again",
                $"\"{notification.BookTitle}\" has been reported by {reporters}. Moderators are reviewing it."),

            _ => (
                "A book you list was reported",
                $"\"{notification.BookTitle}\" was reported by {reporters}. Please check that its details are accurate."),
        };
    }

    private async Task RemoveInvalidPushTokensAsync(
        BookModerationNotification notification,
        IReadOnlyCollection<string> invalidDeviceTokens)
    {
        if (invalidDeviceTokens.Count == 0)
        {
            return;
        }

        try
        {
            // The delivery outcome is already persisted; cleanup must never turn
            // a successful send into a retry.
            await _pushDeviceRepository.RemoveTokensAsync(
                notification.RecipientUserId,
                invalidDeviceTokens,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid FCM tokens could not be removed for book moderation notification {NotificationId}.",
                notification.Id);
        }
    }

    private static DateTime GetNextAttemptAtUtc(int completedAttemptCount)
    {
        var delayIndex = Math.Min(Math.Max(completedAttemptCount, 0), RetryDelays.Length - 1);
        return DateTime.UtcNow.Add(RetryDelays[delayIndex]);
    }
}
