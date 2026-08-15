using MediatR;

namespace Quraaa.Application.Features.Libraries.Commands.DispatchLibraryApprovalNotifications;

public sealed record DispatchLibraryApprovalNotificationsCommand(
    int BatchSize = 20) : IRequest<DispatchLibraryApprovalNotificationsResult>;

public sealed record DispatchLibraryApprovalNotificationsResult(
    int ClaimedCount,
    int EmailSentCount,
    int EmailUncertainCount,
    int PushSentCount,
    int RetryScheduledCount,
    int AbandonedCount);
