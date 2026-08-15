using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Notifications;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
    public sealed class BookModerationNotificationConfiguration
        : IEntityTypeConfiguration<BookModerationNotification>
    {
        public void Configure(EntityTypeBuilder<BookModerationNotification> builder)
        {
            builder.ToTable("BookModerationNotifications");

            builder.HasKey(notification => notification.Id);
            builder.Property(notification => notification.Id).ValueGeneratedNever();

            builder.Property(notification => notification.BookId).IsRequired();
            builder.Property(notification => notification.RecipientUserId).IsRequired();

            builder.Property(notification => notification.Audience)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(notification => notification.Level)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(notification => notification.ReporterCount).IsRequired();

            builder.Property(notification => notification.BookTitle)
                .HasMaxLength(BookModerationNotification.BookTitleMaxLength)
                .IsRequired();

            builder.Property(notification => notification.PushState)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(notification => notification.PushAttemptCount).IsRequired();
            builder.Property(notification => notification.PushNextAttemptAtUtc).IsRequired();
            builder.Property(notification => notification.PushCompletedAtUtc);

            builder.Property(notification => notification.DeliveredPushTokenHashes)
                .IsRequired();

            builder.Property(notification => notification.LeaseUntilUtc);

            builder.Property(notification => notification.ConcurrencyStamp)
                .IsRequired()
                .IsConcurrencyToken();

            // Drives the delivery scan.
            builder.HasIndex(notification => new
            {
                notification.PushState,
                notification.PushNextAttemptAtUtc,
            });

            builder.HasIndex(notification => notification.BookId);

            builder.HasOne<BookAggregate>()
                .WithMany()
                .HasForeignKey(notification => notification.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<UserAggregate>()
                .WithMany()
                .HasForeignKey(notification => notification.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
