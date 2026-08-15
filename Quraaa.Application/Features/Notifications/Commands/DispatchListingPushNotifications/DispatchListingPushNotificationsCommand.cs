using MediatR;

namespace Quraaa.Application.Features.Notifications.Commands.DispatchListingPushNotifications;

public sealed record DispatchListingPushNotificationsCommand(
    int BatchSize = 20) : IRequest<DispatchListingPushNotificationsResult>;

public sealed record DispatchListingPushNotificationsResult(
    int ClaimedCount,
    int CompletedCount,
    int RetryScheduledCount,
    int AbandonedCount);
