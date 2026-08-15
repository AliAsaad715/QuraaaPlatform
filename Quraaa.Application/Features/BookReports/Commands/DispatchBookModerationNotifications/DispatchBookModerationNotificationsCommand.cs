using MediatR;

namespace Quraaa.Application.Features.BookReports.Commands.DispatchBookModerationNotifications;

public sealed record DispatchBookModerationNotificationsCommand(int BatchSize)
    : IRequest<DispatchBookModerationNotificationsResult>;

public sealed record DispatchBookModerationNotificationsResult(
    int ClaimedCount,
    int SentCount,
    int RetryScheduledCount,
    int AbandonedCount);
