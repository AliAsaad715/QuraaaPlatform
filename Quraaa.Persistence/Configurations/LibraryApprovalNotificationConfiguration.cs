using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Library;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.Notifications.Enums;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations;

public sealed class LibraryApprovalNotificationConfiguration
    : IEntityTypeConfiguration<LibraryApprovalNotification>
{
    public void Configure(EntityTypeBuilder<LibraryApprovalNotification> builder)
    {
        builder.ToTable("LibraryApprovalNotifications", table =>
        {
            table.HasCheckConstraint(
                "CK_LibraryApprovalNotifications_EmailState_Valid",
                "\"EmailState\" BETWEEN 1 AND 4");
            table.HasCheckConstraint(
                "CK_LibraryApprovalNotifications_PushState_Valid",
                "\"PushState\" BETWEEN 1 AND 4");
            table.HasCheckConstraint(
                "CK_LibraryApprovalNotifications_AttemptCounts_NonNegative",
                "\"EmailAttemptCount\" >= 0 AND \"PushAttemptCount\" >= 0");
        });

        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).ValueGeneratedNever();
        builder.Property(notification => notification.LibraryId).IsRequired();
        builder.Property(notification => notification.UserId).IsRequired();
        builder.Property(notification => notification.RecipientEmail)
               .HasMaxLength(LibraryApprovalNotification.RecipientEmailMaxLength)
               .IsRequired();
        builder.Property(notification => notification.LibraryName)
               .HasMaxLength(LibraryApprovalNotification.LibraryNameMaxLength)
               .IsRequired();
        builder.Property(notification => notification.EmailState)
               .HasDefaultValue(NotificationDeliveryState.Pending)
               .IsRequired();
        builder.Property(notification => notification.PushState)
               .HasDefaultValue(NotificationDeliveryState.Pending)
               .IsRequired();
        builder.Property(notification => notification.EmailAttemptCount)
               .HasDefaultValue(0)
               .IsRequired();
        builder.Property(notification => notification.PushAttemptCount)
               .HasDefaultValue(0)
               .IsRequired();
        builder.Property(notification => notification.EmailNextAttemptAtUtc).IsRequired();
        builder.Property(notification => notification.PushNextAttemptAtUtc).IsRequired();
        builder.Property(notification => notification.DeliveredPushTokenHashes)
               .HasColumnType("text[]")
               .IsRequired();
        builder.Property(notification => notification.ConcurrencyStamp)
               .IsConcurrencyToken()
               .IsRequired();

        builder.HasOne<LibraryAggregate>()
               .WithMany()
               .HasForeignKey(notification => notification.LibraryId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserAggregate>()
               .WithMany()
               .HasForeignKey(notification => notification.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(notification => notification.LibraryId).IsUnique();
        builder.HasIndex(notification => new
        {
            notification.EmailState,
            notification.EmailNextAttemptAtUtc
        });
        builder.HasIndex(notification => new
        {
            notification.PushState,
            notification.PushNextAttemptAtUtc
        });
        builder.HasIndex(notification => notification.LeaseUntilUtc);
    }
}
