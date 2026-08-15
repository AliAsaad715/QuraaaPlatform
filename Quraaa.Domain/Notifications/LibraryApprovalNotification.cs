using Quraaa.Domain.Notifications.Enums;
using Quraaa.Domain.Shared.Entities;
using Quraaa.Domain.Shared.Exceptions;
using Quraaa.Domain.User.Entities;

namespace Quraaa.Domain.Notifications;

public sealed class LibraryApprovalNotification : AggregateRoot
{
    public const int RecipientEmailMaxLength = 256;
    public const int LibraryNameMaxLength = 100;

    public Guid LibraryId { get; private set; }
    public Guid UserId { get; private set; }
    public string RecipientEmail { get; private set; } = null!;
    public string LibraryName { get; private set; } = null!;
    public NotificationDeliveryState EmailState { get; private set; }
    public NotificationDeliveryState PushState { get; private set; }
    public int EmailAttemptCount { get; private set; }
    public int PushAttemptCount { get; private set; }
    public DateTime EmailNextAttemptAtUtc { get; private set; }
    public DateTime PushNextAttemptAtUtc { get; private set; }
    public DateTime? EmailCompletedAtUtc { get; private set; }
    public DateTime? PushCompletedAtUtc { get; private set; }
    public string[] DeliveredPushTokenHashes { get; private set; } = [];
    public DateTime? LeaseUntilUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; }

    private LibraryApprovalNotification() { }

    private LibraryApprovalNotification(
        Guid id,
        Guid libraryId,
        Guid userId,
        string recipientEmail,
        string libraryName,
        DateTime utcNow)
    {
        if (id == Guid.Empty || libraryId == Guid.Empty || userId == Guid.Empty)
        {
            throw new DomainException("Approval notification identifiers are required.");
        }

        var normalizedEmail = recipientEmail?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail)
            || normalizedEmail.Length > RecipientEmailMaxLength)
        {
            throw new DomainException("A valid approval notification recipient is required.");
        }

        var normalizedLibraryName = libraryName?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedLibraryName)
            || normalizedLibraryName.Length > LibraryNameMaxLength)
        {
            throw new DomainException("A valid library name is required for the approval notification.");
        }

        var normalizedNow = NormalizeUtc(utcNow);

        Id = id;
        LibraryId = libraryId;
        UserId = userId;
        RecipientEmail = normalizedEmail;
        LibraryName = normalizedLibraryName;
        EmailState = NotificationDeliveryState.Pending;
        PushState = NotificationDeliveryState.Pending;
        EmailNextAttemptAtUtc = normalizedNow;
        PushNextAttemptAtUtc = normalizedNow;
        ConcurrencyStamp = Guid.NewGuid();
    }

    public static LibraryApprovalNotification Create(
        Guid libraryId,
        Guid userId,
        string recipientEmail,
        string libraryName,
        DateTime utcNow) =>
        new(
            Guid.NewGuid(),
            libraryId,
            userId,
            recipientEmail,
            libraryName,
            utcNow);

    public bool IsReady(DateTime utcNow)
    {
        var normalizedNow = NormalizeUtc(utcNow);
        return (LeaseUntilUtc is null || LeaseUntilUtc <= normalizedNow)
            && ((EmailState == NotificationDeliveryState.Pending
                 && EmailNextAttemptAtUtc <= normalizedNow)
                || (PushState == NotificationDeliveryState.Pending
                    && PushNextAttemptAtUtc <= normalizedNow));
    }

    public bool ShouldAttemptEmail(DateTime utcNow) =>
        EmailState == NotificationDeliveryState.Pending
        && EmailNextAttemptAtUtc <= NormalizeUtc(utcNow);

    public bool ShouldAttemptPush(DateTime utcNow) =>
        PushState == NotificationDeliveryState.Pending
        && PushNextAttemptAtUtc <= NormalizeUtc(utcNow);

    public bool WasPushDeliveredTo(string deviceToken)
    {
        var tokenHash = PushDevice.ComputeTokenHash(deviceToken);
        return DeliveredPushTokenHashes.Contains(tokenHash, StringComparer.Ordinal);
    }

    public bool HasAnySuccessfulPushDelivery => DeliveredPushTokenHashes.Length > 0;

    public void Claim(DateTime utcNow, TimeSpan leaseDuration)
    {
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new DomainException("The notification delivery lease must be positive.");
        }

        var normalizedNow = NormalizeUtc(utcNow);
        if (!IsReady(normalizedNow))
        {
            throw new DomainException("The approval notification is not ready to be claimed.");
        }

        LeaseUntilUtc = normalizedNow.Add(leaseDuration);
        RenewConcurrencyStamp();
    }

    public void MarkEmailSent(DateTime utcNow)
    {
        EnsurePending(EmailState, "email");
        EmailAttemptCount++;
        EmailState = NotificationDeliveryState.Sent;
        EmailCompletedAtUtc = NormalizeUtc(utcNow);
        RenewConcurrencyStamp();
    }

    public void MarkEmailDeliveryUncertain(DateTime utcNow)
    {
        EnsurePending(EmailState, "email");
        EmailAttemptCount++;
        EmailState = NotificationDeliveryState.DeliveryUncertain;
        EmailCompletedAtUtc = NormalizeUtc(utcNow);
        RenewConcurrencyStamp();
    }

    public void RecordEmailFailure(
        DateTime utcNow,
        DateTime nextAttemptAtUtc,
        int maximumAttempts)
    {
        EnsurePending(EmailState, "email");
        EmailAttemptCount++;
        if (EmailAttemptCount >= maximumAttempts)
        {
            EmailState = NotificationDeliveryState.Abandoned;
            EmailCompletedAtUtc = NormalizeUtc(utcNow);
        }
        else
        {
            EmailNextAttemptAtUtc = NormalizeUtc(nextAttemptAtUtc);
        }

        RenewConcurrencyStamp();
    }

    public void MarkPushSent(DateTime utcNow)
    {
        EnsurePending(PushState, "push");
        PushState = NotificationDeliveryState.Sent;
        PushCompletedAtUtc = NormalizeUtc(utcNow);
        RenewConcurrencyStamp();
    }

    public void RecordPushAttempt(
        IReadOnlyCollection<string> successfulDeviceTokens,
        bool hasTransientFailures,
        DateTime utcNow,
        DateTime nextAttemptAtUtc,
        int maximumAttempts)
    {
        EnsurePending(PushState, "push");
        PushAttemptCount++;

        if (successfulDeviceTokens.Count > 0)
        {
            DeliveredPushTokenHashes = DeliveredPushTokenHashes
                .Concat(successfulDeviceTokens.Select(PushDevice.ComputeTokenHash))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        if (HasAnySuccessfulPushDelivery && !hasTransientFailures)
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

    public void ReleaseLease()
    {
        LeaseUntilUtc = null;
        RenewConcurrencyStamp();
    }

    private static void EnsurePending(
        NotificationDeliveryState state,
        string channel)
    {
        if (state != NotificationDeliveryState.Pending)
        {
            throw new DomainException(
                $"The approval notification {channel} channel is already complete.");
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
