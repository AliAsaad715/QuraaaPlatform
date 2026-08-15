using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.BookReports.Interfaces;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.Notifications.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories;

public class BookModerationNotificationRepository : IBookModerationNotificationRepository
{
    private readonly ApplicationDbContext _context;

    public BookModerationNotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<BookModerationNotification> notifications,
        CancellationToken cancellationToken = default)
    {
        await _context.BookModerationNotifications.AddRangeAsync(notifications, cancellationToken);
    }

    public async Task<IReadOnlyList<BookModerationNotification>> ClaimReadyAsync(
        DateTime utcNow,
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "The moderation notification batch size must be between 1 and 100.");
        }

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration),
                "The moderation notification lease duration must be positive.");
        }

        var normalizedNow = NormalizeUtc(utcNow);
        var pendingState = (int)NotificationDeliveryState.Pending;

        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        // SKIP LOCKED so replicas can drain the outbox side by side.
        var notifications = await _context.BookModerationNotifications
            .FromSqlInterpolated($$"""
                SELECT *
                FROM "BookModerationNotifications"
                WHERE NOT "IsDeleted"
                  AND ("LeaseUntilUtc" IS NULL OR "LeaseUntilUtc" <= {{normalizedNow}})
                  AND "PushState" = {{pendingState}}
                  AND "PushNextAttemptAtUtc" <= {{normalizedNow}}
                ORDER BY "CreationTime", "Id"
                LIMIT {{batchSize}}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.Claim(normalizedNow, leaseDuration);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return notifications;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
