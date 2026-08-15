using Quraaa.Domain.Notifications.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.User.Entities;

namespace Quraaa.Domain.Notifications;

/// <summary>
/// Outbox row for one recipient of a book moderation notice. Written in the
/// same transaction as the report that triggered it, then delivered by a
/// background service — so a push outage can never fail the report itself.
/// </summary>
public sealed class BookModerationNotification : AggregateRoot
{
    public const int BookTitleMaxLength = 500;

    public Guid BookId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public BookModerationAudience Audience { get; private set; }
    public BookModerationLevel Level { get; private set; }

    /// <summary>Distinct reporters at the moment the notice was raised.</summary>
    public int ReporterCount { get; private set; }

    public string BookTitle { get; private set; } = null!;
    public NotificationDeliveryState PushState { get; private set; }
    public int PushAttemptCount { get; private set; }
    public DateTime PushNextAttemptAtUtc { get; private set; }
    public DateTime? PushCompletedAtUtc { get; private set; }
    public string[] DeliveredPushTokenHashes { get; private set; } = [];
    public DateTime? LeaseUntilUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; }

    private BookModerationNotification() { }

    private BookModerationNotification(
        Guid bookId,
        Guid recipientUserId,
        BookModerationAudience audience,
        BookModerationLevel level,
        int reporterCount,
        string bookTitle,
        DateTime utcNow)
    {
        if (bookId == Guid.Empty || recipientUserId == Guid.Empty)
        {
            throw new DomainException("Book moderation notification identifiers are required.");
        }

        if (reporterCount < 1)
        {
            throw new DomainException("A moderation notice needs at least one reporter.");
        }

        var normalizedTitle = bookTitle?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            throw new DomainException("A book title is required for the moderation notice.");
        }

        if (normalizedTitle.Length > BookTitleMaxLength)
        {
            normalizedTitle = normalizedTitle[..BookTitleMaxLength];
        }

        var normalizedNow = NormalizeUtc(utcNow);

        Id = Guid.NewGuid();
        BookId = bookId;
        RecipientUserId = recipientUserId;
        Audience = audience;
        Level = level;
        ReporterCount = reporterCount;
        BookTitle = normalizedTitle;
        PushState = NotificationDeliveryState.Pending;
        PushNextAttemptAtUtc = normalizedNow;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public static BookModerationNotification Create(
        Guid bookId,
        Guid recipientUserId,
        BookModerationAudience audience,
        BookModerationLevel level,
        int reporterCount,
        string bookTitle,
        DateTime utcNow) =>
        new(bookId, recipientUserId, audience, level, reporterCount, bookTitle, utcNow);

    public bool IsReady(DateTime utcNow)
    {
        var normalizedNow = NormalizeUtc(utcNow);
        return (LeaseUntilUtc is null || LeaseUntilUtc <= normalizedNow)
            && PushState == NotificationDeliveryState.Pending
            && PushNextAttemptAtUtc <= normalizedNow;
    }

    public bool WasDeliveredTo(string deviceToken) =>
        DeliveredPushTokenHashes.Contains(
            PushDevice.ComputeTokenHash(deviceToken),
            StringComparer.Ordinal);

    public void Claim(DateTime utcNow, TimeSpan leaseDuration)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new DomainException("The notification delivery lease must be positive.");
        }

        var normalizedNow = NormalizeUtc(utcNow);
        if (!IsReady(normalizedNow))
        {
            throw new DomainException("The moderation notification is not ready to be claimed.");
        }

        LeaseUntilUtc = normalizedNow.Add(leaseDuration);
        RenewConcurrencyStamp();
    }

    /// <summary>
    /// Records the outcome of one push attempt. Mirrors the library approval
    /// notification: any successful device with no transient failure completes
    /// the notice; otherwise it retries until the attempt budget runs out.
    /// </summary>
    public void RecordPushAttempt(
        IReadOnlyCollection<string> successfulDeviceTokens,
        bool hasTransientFailures,
        DateTime utcNow,
        DateTime nextAttemptAtUtc,
        int maximumAttempts)
    {
        if (PushState != NotificationDeliveryState.Pending)
        {
            throw new DomainException("The moderation notification is already complete.");
        }

        PushAttemptCount++;

        if (successfulDeviceTokens.Count > 0)
        {
            DeliveredPushTokenHashes = DeliveredPushTokenHashes
                .Concat(successfulDeviceTokens.Select(PushDevice.ComputeTokenHash))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        if (DeliveredPushTokenHashes.Length > 0 && !hasTransientFailures)
        {
            PushState = NotificationDeliveryState.Sent;
            PushCompletedAtUtc = NormalizeUtc(utcNow);
        }
        else if (PushAttemptCount >= maximumAttempts)
        {
            PushState = NotificationDeliveryState.Abandoned;
            PushCompletedAtUtc = NormalizeUtc(utcNow);
        }
        else
        {
            PushNextAttemptAtUtc = NormalizeUtc(nextAttemptAtUtc);
        }

        RenewConcurrencyStamp();
    }

    /// <summary>The recipient has no registered device; nothing to deliver.</summary>
    public void MarkUndeliverable(DateTime utcNow)
    {
        if (PushState != NotificationDeliveryState.Pending)
        {
            return;
        }

        PushAttemptCount++;
        PushState = NotificationDeliveryState.Abandoned;
        PushCompletedAtUtc = NormalizeUtc(utcNow);
        RenewConcurrencyStamp();
    }

    public void ReleaseLease()
    {
        LeaseUntilUtc = null;
        RenewConcurrencyStamp();
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
