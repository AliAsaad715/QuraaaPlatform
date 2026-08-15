using MediatR;
using Microsoft.Extensions.Logging;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Notifications.Common;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.Notifications.Enums;

namespace Quraaa.Application.Features.Libraries.Commands.DispatchLibraryApprovalNotifications;

public sealed class DispatchLibraryApprovalNotificationsCommandHandler
    : IRequestHandler<
        DispatchLibraryApprovalNotificationsCommand,
        DispatchLibraryApprovalNotificationsResult>
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

    private readonly ILibraryApprovalNotificationRepository _notificationRepository;
    private readonly IPushDeviceRepository _pushDeviceRepository;
    private readonly ILibraryEmailSender _emailSender;
    private readonly IFirebaseNotificationService _firebaseNotificationService;
    private readonly ILogger<DispatchLibraryApprovalNotificationsCommandHandler> _logger;

    public DispatchLibraryApprovalNotificationsCommandHandler(
        ILibraryApprovalNotificationRepository notificationRepository,
        IPushDeviceRepository pushDeviceRepository,
        ILibraryEmailSender emailSender,
        IFirebaseNotificationService firebaseNotificationService,
        ILogger<DispatchLibraryApprovalNotificationsCommandHandler> logger)
    {
        _notificationRepository = notificationRepository;
        _pushDeviceRepository = pushDeviceRepository;
        _emailSender = emailSender;
        _firebaseNotificationService = firebaseNotificationService;
        _logger = logger;
    }

    public async Task<DispatchLibraryApprovalNotificationsResult> Handle(
        DispatchLibraryApprovalNotificationsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BatchSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.BatchSize),
                "The approval notification batch size must be between 1 and 100.");
        }

        var claimedCount = 0;
        var emailSentCount = 0;
        var emailUncertainCount = 0;
        var pushSentCount = 0;
        var retryScheduledCount = 0;
        var abandonedCount = 0;

        for (var index = 0; index < request.BatchSize; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Claim only the item that is about to be delivered. Leasing a full
            // batch before sequential provider calls lets later leases expire and
            // be reclaimed by another application instance.
            var utcNow = DateTime.UtcNow;
            var claimedNotifications = await _notificationRepository.ClaimReadyAsync(
                utcNow,
                1,
                DeliveryLeaseDuration,
                cancellationToken);
            if (claimedNotifications.Count == 0)
            {
                break;
            }

            var notification = claimedNotifications[0];
            claimedCount++;

            if (notification.ShouldAttemptEmail(utcNow))
            {
                var previousState = notification.EmailState;
                var emailResult = await DeliverEmailAsync(notification, cancellationToken);
                emailSentCount += emailResult == NotificationDeliveryState.Sent ? 1 : 0;
                emailUncertainCount += emailResult == NotificationDeliveryState.DeliveryUncertain ? 1 : 0;
                retryScheduledCount += previousState == NotificationDeliveryState.Pending
                    && emailResult == NotificationDeliveryState.Pending
                        ? 1
                        : 0;
                abandonedCount += emailResult == NotificationDeliveryState.Abandoned ? 1 : 0;

                // Persist the email outcome before starting push delivery. The two
                // providers are independent; a later FCM failure must not cause an
                // already accepted SMTP message to be retried.
                await _notificationRepository.SaveChangesAsync(CancellationToken.None);
            }

            if (notification.ShouldAttemptPush(utcNow))
            {
                var previousState = notification.PushState;
                var pushOutcome = await DeliverPushAsync(notification, cancellationToken);
                var pushResult = pushOutcome.State;
                pushSentCount += pushResult == NotificationDeliveryState.Sent ? 1 : 0;
                retryScheduledCount += previousState == NotificationDeliveryState.Pending
                    && pushResult == NotificationDeliveryState.Pending
                        ? 1
                        : 0;
                abandonedCount += pushResult == NotificationDeliveryState.Abandoned ? 1 : 0;

                await _notificationRepository.SaveChangesAsync(CancellationToken.None);
                await RemoveInvalidPushTokensAsync(
                    notification,
                    pushOutcome.InvalidDeviceTokens);
            }

            notification.ReleaseLease();
            await _notificationRepository.SaveChangesAsync(CancellationToken.None);
        }

        return new DispatchLibraryApprovalNotificationsResult(
            claimedCount,
            emailSentCount,
            emailUncertainCount,
            pushSentCount,
            retryScheduledCount,
            abandonedCount);
    }

    private async Task<NotificationDeliveryState> DeliverEmailAsync(
        LibraryApprovalNotification notification,
        CancellationToken cancellationToken)
    {
        EmailDeliveryStatus deliveryStatus;
        try
        {
            deliveryStatus = await _emailSender.SendApprovalAsync(
                notification.RecipientEmail,
                notification.LibraryName,
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
                "Library approval email delivery failed before it returned a classified outcome for notification {NotificationId}.",
                notification.Id);
            ScheduleEmailRetry(notification);
            return notification.EmailState;
        }

        switch (deliveryStatus)
        {
            case EmailDeliveryStatus.Sent:
                notification.MarkEmailSent(DateTime.UtcNow);
                break;
            case EmailDeliveryStatus.Unknown:
                // SMTP may already have accepted the message. Do not retry an
                // ambiguous outcome because that can duplicate the email.
                notification.MarkEmailDeliveryUncertain(DateTime.UtcNow);
                _logger.LogWarning(
                    "Library approval email delivery is uncertain for notification {NotificationId}; automatic retry is suppressed.",
                    notification.Id);
                break;
            case EmailDeliveryStatus.NotSent:
                ScheduleEmailRetry(notification);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported email delivery status: {deliveryStatus}.");
        }

        return notification.EmailState;
    }

    private async Task<PushDeliveryOutcome> DeliverPushAsync(
        LibraryApprovalNotification notification,
        CancellationToken cancellationToken)
    {
        PushBatchDeliveryResult result;
        try
        {
            var registeredDeviceTokens = await _pushDeviceRepository.GetTokensByUserIdAsync(
                notification.UserId,
                cancellationToken);
            var deviceTokens = registeredDeviceTokens
                .Where(token => !notification.WasPushDeliveredTo(token))
                .ToArray();

            if (deviceTokens.Length == 0)
            {
                if (notification.HasAnySuccessfulPushDelivery)
                {
                    notification.MarkPushSent(DateTime.UtcNow);
                }
                else
                {
                    SchedulePushRetry(notification);
                }

                return new PushDeliveryOutcome(notification.PushState, []);
            }

            result = await _firebaseNotificationService.SendToDevicesAsync(
                deviceTokens,
                "Library registration approved",
                "Your library registration has been approved. Sign in again to continue.",
                new Dictionary<string, string>
                {
                    ["type"] = "library_registration_approved",
                    ["libraryId"] = notification.LibraryId.ToString("D"),
                    ["approvalStatus"] = "Approved",
                    ["requiresReauthentication"] = "true"
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
                "Library approval push delivery failed for notification {NotificationId}.",
                notification.Id);
            SchedulePushRetry(notification);
            return new PushDeliveryOutcome(notification.PushState, []);
        }

        notification.RecordPushAttempt(
            result.SuccessfulDeviceTokens,
            result.HasTransientFailures,
            DateTime.UtcNow,
            GetNextAttemptAtUtc(notification.PushAttemptCount),
            MaximumDeliveryAttempts);

        return new PushDeliveryOutcome(
            notification.PushState,
            result.InvalidDeviceTokens);
    }

    private async Task RemoveInvalidPushTokensAsync(
        LibraryApprovalNotification notification,
        IReadOnlyCollection<string> invalidDeviceTokens)
    {
        if (invalidDeviceTokens.Count == 0)
        {
            return;
        }

        try
        {
            // The FCM outcome has already been persisted. Cleanup must not turn
            // successful deliveries into retries if this local delete fails.
            await _pushDeviceRepository.RemoveTokensAsync(
                notification.UserId,
                invalidDeviceTokens,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Permanently invalid FCM tokens could not be removed for approval notification {NotificationId}; the delivery outcome remains recorded.",
                notification.Id);
        }
    }

    private static void ScheduleEmailRetry(LibraryApprovalNotification notification)
    {
        notification.RecordEmailFailure(
            DateTime.UtcNow,
            GetNextAttemptAtUtc(notification.EmailAttemptCount),
            MaximumDeliveryAttempts);
    }

    private static void SchedulePushRetry(LibraryApprovalNotification notification)
    {
        notification.RecordPushAttempt(
            [],
            true,
            DateTime.UtcNow,
            GetNextAttemptAtUtc(notification.PushAttemptCount),
            MaximumDeliveryAttempts);
    }

    private static DateTime GetNextAttemptAtUtc(int completedAttemptCount)
    {
        var delayIndex = Math.Min(completedAttemptCount, RetryDelays.Length - 1);
        return DateTime.UtcNow.Add(RetryDelays[delayIndex]);
    }

    private sealed record PushDeliveryOutcome(
        NotificationDeliveryState State,
        IReadOnlyCollection<string> InvalidDeviceTokens);
}
