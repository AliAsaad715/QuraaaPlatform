using Quraaa.Domain.Notifications.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.User.Entities;

namespace Quraaa.Domain.Notifications;

public sealed class ListingPushNotification : AggregateRoot
{
    public Guid LibraryId { get; private set; }
    public ListingPushNotificationType Type { get; private set; }
    public Guid? BookId { get; private set; }
    public Guid? ListingId { get; private set; }
    public int ItemCount { get; private set; }
    public NotificationDeliveryState State { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime NextAttemptAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string[] DeliveredPushTokenHashes { get; private set; } = [];
    public DateTime? LeaseUntilUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; }

    public bool IsBatchPublication =>
        Type == ListingPushNotificationType.LibraryListingsPublished
        && ItemCount > 1;

    private ListingPushNotification() { }

    private ListingPushNotification(
        Guid id,
        Guid libraryId,
        ListingPushNotificationType type,
        Guid? bookId,
        Guid? listingId,
        int itemCount,
        DateTime utcNow)
    {
        if (id == Guid.Empty || libraryId == Guid.Empty)
        {
            throw new DomainException("Listing notification identifiers are required.");
        }

        if (itemCount <= 0)
        {
            throw new DomainException("Listing notification item count must be positive.");
        }

        ValidatePayload(type, bookId, listingId, itemCount);

        var normalizedNow = NormalizeUtc(utcNow);

        Id = id;
        LibraryId = libraryId;
        Type = type;
        BookId = bookId;
        ListingId = listingId;
        ItemCount = itemCount;
        State = NotificationDeliveryState.Pending;
        NextAttemptAtUtc = normalizedNow;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public static ListingPushNotification CreatePublication(
        Guid libraryId,
        Guid? bookId,
        Guid? listingId,
        int itemCount,
        DateTime utcNow) =>
        new(
            Guid.NewGuid(),
            libraryId,
            ListingPushNotificationType.LibraryListingsPublished,
            bookId,
            listingId,
            itemCount,
            utcNow);

    public static ListingPushNotification CreateDigitalAssetUpdate(
        Guid libraryId,
        Guid bookId,
        Guid listingId,
        DateTime utcNow) =>
        new(
            Guid.NewGuid(),
            libraryId,
            ListingPushNotificationType.ListingDigitalAssetUpdated,
            bookId,
            listingId,
            1,
            utcNow);

    public bool IsReady(DateTime utcNow)
    {
        var normalizedNow = NormalizeUtc(utcNow);
        return State == NotificationDeliveryState.Pending
            && NextAttemptAtUtc <= normalizedNow
            && (LeaseUntilUtc is null || LeaseUntilUtc <= normalizedNow);
    }

    public bool WasDeliveredTo(string deviceToken)
    {
        var tokenHash = PushDevice.ComputeTokenHash(deviceToken);
        return DeliveredPushTokenHashes.Contains(tokenHash, StringComparer.Ordinal);
    }

    public void Claim(DateTime utcNow, TimeSpan leaseDuration)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new DomainException("The listing notification delivery lease must be positive.");
        }

        var normalizedNow = NormalizeUtc(utcNow);
        if (!IsReady(normalizedNow))
        {
            throw new DomainException("The listing notification is not ready to be claimed.");
        }

        LeaseUntilUtc = normalizedNow.Add(leaseDuration);
        RenewConcurrencyStamp();
    }

    public void MarkCompleted(DateTime utcNow)
    {
        EnsurePending();
        State = NotificationDeliveryState.Sent;
        CompletedAtUtc = NormalizeUtc(utcNow);
        RenewConcurrencyStamp();
    }

    public void MarkAbandoned(DateTime utcNow)
    {
        EnsurePending();
        State = NotificationDeliveryState.Abandoned;
        CompletedAtUtc = NormalizeUtc(utcNow);
        RenewConcurrencyStamp();
    }

    public void RecordPushAttempt(
        IReadOnlyCollection<string> successfulDeviceTokens,
        bool hasTransientFailures,
        DateTime utcNow,
        DateTime nextAttemptAtUtc,
        int maximumAttempts)
    {
        EnsurePending();

        if (maximumAttempts <= 0)
        {
            throw new DomainException("Maximum listing notification attempts must be positive.");
        }

        AttemptCount++;

        if (successfulDeviceTokens.Count > 0)
        {
            DeliveredPushTokenHashes = DeliveredPushTokenHashes
                .Concat(successfulDeviceTokens.Select(PushDevice.ComputeTokenHash))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        if (!hasTransientFailures)
        {
            State = NotificationDeliveryState.Sent;
            CompletedAtUtc = NormalizeUtc(utcNow);
        }
        else if (AttemptCount >= maximumAttempts)
        {
            State = NotificationDeliveryState.Abandoned;
            CompletedAtUtc = NormalizeUtc(utcNow);
        }
        else
        {
            NextAttemptAtUtc = NormalizeUtc(nextAttemptAtUtc);
        }

        RenewConcurrencyStamp();
    }

    public void ReleaseLease()
    {
        LeaseUntilUtc = null;
        RenewConcurrencyStamp();
    }

    private void EnsurePending()
    {
        if (State != NotificationDeliveryState.Pending)
        {
            throw new DomainException("The listing notification delivery is already complete.");
        }
    }

    private static void ValidatePayload(
        ListingPushNotificationType type,
        Guid? bookId,
        Guid? listingId,
        int itemCount)
    {
        var hasBookId = bookId.HasValue && bookId.Value != Guid.Empty;
        var hasListingId = listingId.HasValue && listingId.Value != Guid.Empty;

        switch (type)
        {
            case ListingPushNotificationType.LibraryListingsPublished
                when itemCount == 1 && hasBookId && hasListingId:
            case ListingPushNotificationType.LibraryListingsPublished
                when itemCount > 1 && !bookId.HasValue && !listingId.HasValue:
            case ListingPushNotificationType.ListingDigitalAssetUpdated
                when itemCount == 1 && hasBookId && hasListingId:
                return;
            default:
                throw new DomainException("The listing notification payload is invalid.");
        }
    }

    private void RenewConcurrencyStamp()
    {
        ConcurrencyStamp = Guid.NewGuid();
        UpdateModificationTime();
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
