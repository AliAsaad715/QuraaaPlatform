using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Library;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.Notifications.Enums;

namespace Quraaa.Persistence.Configurations;

public sealed class ListingPushNotificationConfiguration
    : IEntityTypeConfiguration<ListingPushNotification>
{
    public void Configure(EntityTypeBuilder<ListingPushNotification> builder)
    {
        builder.ToTable("ListingPushNotifications", table =>
        {
            table.HasCheckConstraint(
                "CK_ListingPushNotifications_Type_Valid",
                "\"Type\" BETWEEN 1 AND 2");
            table.HasCheckConstraint(
                "CK_ListingPushNotifications_State_Valid",
                "\"State\" BETWEEN 1 AND 4");
            table.HasCheckConstraint(
                "CK_ListingPushNotifications_AttemptCount_NonNegative",
                "\"AttemptCount\" >= 0");
            table.HasCheckConstraint(
                "CK_ListingPushNotifications_ItemCount_Positive",
                "\"ItemCount\" > 0");
            table.HasCheckConstraint(
                "CK_ListingPushNotifications_Payload_Valid",
                "(\"Type\" = 1 AND ((\"ItemCount\" = 1 AND \"BookId\" IS NOT NULL AND \"ListingId\" IS NOT NULL) OR (\"ItemCount\" > 1 AND \"BookId\" IS NULL AND \"ListingId\" IS NULL))) OR (\"Type\" = 2 AND \"ItemCount\" = 1 AND \"BookId\" IS NOT NULL AND \"ListingId\" IS NOT NULL)");
        });

        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).ValueGeneratedNever();
        builder.Property(notification => notification.LibraryId).IsRequired();
        builder.Property(notification => notification.Type).IsRequired();
        builder.Property(notification => notification.ItemCount).IsRequired();
        builder.Property(notification => notification.State)
            .HasDefaultValue(NotificationDeliveryState.Pending)
            .IsRequired();
        builder.Property(notification => notification.AttemptCount)
            .HasDefaultValue(0)
            .IsRequired();
        builder.Property(notification => notification.NextAttemptAtUtc).IsRequired();
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
        builder.HasOne<BookAggregate>()
            .WithMany()
            .HasForeignKey(notification => notification.BookId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ListingAggregate>()
            .WithMany()
            .HasForeignKey(notification => notification.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(notification => notification.LibraryId);
        builder.HasIndex(notification => new
        {
            notification.State,
            notification.NextAttemptAtUtc
        });
        builder.HasIndex(notification => notification.LeaseUntilUtc);
    }
}
