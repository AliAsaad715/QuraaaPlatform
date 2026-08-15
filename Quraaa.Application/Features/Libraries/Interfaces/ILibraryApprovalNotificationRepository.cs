using Quraaa.Domain.Notifications;

namespace Quraaa.Application.Features.Libraries.Interfaces;

public interface ILibraryApprovalNotificationRepository
{
    Task AddAsync(
        LibraryApprovalNotification notification,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LibraryApprovalNotification>> ClaimReadyAsync(
        DateTime utcNow,
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
