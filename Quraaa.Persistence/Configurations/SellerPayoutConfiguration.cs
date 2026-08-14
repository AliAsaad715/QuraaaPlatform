using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Library;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Payouts;

namespace Quraaa.Persistence.Configurations
{
    public sealed class SellerPayoutConfiguration
        : IEntityTypeConfiguration<SellerPayoutAggregate>
    {
        public void Configure(EntityTypeBuilder<SellerPayoutAggregate> builder)
        {
            builder.ToTable("SellerPayouts", table =>
            {
                table.HasCheckConstraint(
                    "CK_SellerPayouts_GrossAmountMinor_Positive",
                    "\"GrossAmountMinor\" > 0");

                table.HasCheckConstraint(
                    "CK_SellerPayouts_NetAmountMinor_Positive",
                    "\"NetAmountMinor\" > 0");

                table.HasCheckConstraint(
                    "CK_SellerPayouts_PlatformFeeMinor_NonNegative",
                    "\"PlatformFeeMinor\" >= 0");

                table.HasCheckConstraint(
                    "CK_SellerPayouts_Amounts_Consistent",
                    "\"GrossAmountMinor\" = \"NetAmountMinor\" + \"PlatformFeeMinor\"");

                table.HasCheckConstraint(
                    "CK_SellerPayouts_CommissionPercent_Range",
                    "\"CommissionPercent\" >= 0 AND \"CommissionPercent\" <= 100");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.OrderId).IsRequired();
            builder.Property(x => x.LibraryId).IsRequired();

            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(x => x.GrossAmountMinor).IsRequired();

            builder.Property(x => x.CommissionPercent)
                .HasColumnType("decimal(5,2)")
                .IsRequired();

            builder.Property(x => x.PlatformFeeMinor).IsRequired();
            builder.Property(x => x.NetAmountMinor).IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.AttemptCount).IsRequired();
            builder.Property(x => x.NextAttemptAtUtc).IsRequired();
            builder.Property(x => x.LastAttemptAtUtc);
            builder.Property(x => x.PaidAtUtc);

            builder.Property(x => x.StripeTransferId)
                .HasMaxLength(255);

            builder.Property(x => x.DestinationStripeAccountId)
                .HasMaxLength(255);

            builder.Property(x => x.FailureReason)
                .HasMaxLength(1000);

            builder.Property(x => x.LastModificationTime)
                .IsConcurrencyToken();

            // One payout per seller per order: the race-proof idempotency
            // guard for concurrent webhook/reconciliation finalization.
            builder.HasIndex(x => new { x.OrderId, x.LibraryId })
                .IsUnique();

            // Drives the background processor's due-payout scan.
            builder.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });

            // Drives the owner-facing payout history (also covers the
            // LibraryId foreign key).
            builder.HasIndex(x => new { x.LibraryId, x.CreationTime });

            builder.HasIndex(x => x.StripeTransferId)
                .IsUnique()
                .HasFilter("\"StripeTransferId\" IS NOT NULL");

            builder.HasOne<LibraryAggregate>()
                .WithMany()
                .HasForeignKey(x => x.LibraryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<OrderAggregate>()
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
