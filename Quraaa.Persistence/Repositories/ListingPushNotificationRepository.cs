using Microsoft.EntityFrameworkCore;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.Notifications.Enums;
using Quraaa.Persistence.Data;

namespace Quraaa.Persistence.Repositories;

public sealed class ListingPushNotificationRepository
    : IListingPushNotificationRepository
{
    private readonly ApplicationDbContext _context;

    public ListingPushNotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ListingPushNotification>> ClaimReadyAsync(
        DateTime utcNow,
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        if (batchSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batchSize),
                "The listing notification batch size must be between 1 and 100.");
        }

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration),
                "The listing notification lease duration must be positive.");
        }

        var normalizedNow = NormalizeUtc(utcNow);
        var pendingState = (int)NotificationDeliveryState.Pending;

        await using var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken);

        var notifications = await _context.ListingPushNotifications
            .FromSqlInterpolated($$"""
                SELECT *
                FROM "ListingPushNotifications"
                WHERE NOT "IsDeleted"
                  AND "State" = {{pendingState}}
                  AND "NextAttemptAtUtc" <= {{normalizedNow}}
                  AND ("LeaseUntilUtc" IS NULL OR "LeaseUntilUtc" <= {{normalizedNow}})
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

    public async Task AddAsync(
        ListingPushNotification notification,
        CancellationToken cancellationToken = default)
    {
        await _context.ListingPushNotifications.AddAsync(notification, cancellationToken);
    }

    public ListingPushNotification? FindStagedPublication(Guid libraryId) =>
        FindStaged(notification =>
            notification.Type == ListingPushNotificationType.LibraryListingsPublished
            && notification.LibraryId == libraryId);

    public ListingPushNotification? FindStagedDigitalAssetUpdate(Guid listingId) =>
        FindStaged(notification =>
            notification.Type == ListingPushNotificationType.ListingDigitalAssetUpdated
            && notification.ListingId == listingId);

    private ListingPushNotification? FindStaged(Func<ListingPushNotification, bool> predicate) =>
        _context.ChangeTracker
            .Entries<ListingPushNotification>()
            .Where(entry => entry.State == EntityState.Added)
            .Select(entry => entry.Entity)
            .FirstOrDefault(predicate);

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
