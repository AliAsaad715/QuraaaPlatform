using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Orders;
using Quraaa.Domain.Orders.Enums;

namespace Quraaa.Persistence.Configurations
{
    public sealed class OrderConfiguration : IEntityTypeConfiguration<OrderAggregate>
    {
        public void Configure(EntityTypeBuilder<OrderAggregate> builder)
        {
            builder.ToTable("Orders", table =>
            {
                table.HasCheckConstraint(
                    "CK_Orders_SubtotalAmountMinor_Positive",
                    "\"SubtotalAmountMinor\" > 0");

                table.HasCheckConstraint(
                    "CK_Orders_TotalAmountMinor_Positive",
                    "\"TotalAmountMinor\" > 0");

                table.HasCheckConstraint(
                    "CK_Orders_ShippingAmountMinor_NonNegative",
                    "\"ShippingAmountMinor\" >= 0");

                table.HasCheckConstraint(
                    "CK_Orders_DiscountAmountMinor_NonNegative",
                    "\"DiscountAmountMinor\" >= 0");

                table.HasCheckConstraint(
                    "CK_Orders_TotalAmountMinor_Consistent",
                    "\"TotalAmountMinor\" = " +
                    "\"SubtotalAmountMinor\" + \"ShippingAmountMinor\" - \"DiscountAmountMinor\"");

                table.HasCheckConstraint(
                    "CK_Orders_ShippingLocation_Valid",
                    "(\"ShippingLatitude\" IS NULL AND \"ShippingLongitude\" IS NULL) OR " +
                    "(\"ShippingLatitude\" BETWEEN -90 AND 90 AND " +
                    "\"ShippingLongitude\" BETWEEN -180 AND 180)");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.BuyerUserId).IsRequired();
            builder.Property(x => x.SourceCartId).IsRequired();

            builder.Property(x => x.OrderNumber)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Currency)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.PaymentStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.SubtotalAmountMinor).IsRequired();
            builder.Property(x => x.ShippingAmountMinor).IsRequired();
            builder.Property(x => x.DiscountAmountMinor).IsRequired();
            builder.Property(x => x.TotalAmountMinor).IsRequired();

            builder.Property(x => x.ShippingLatitude);
            builder.Property(x => x.ShippingLongitude);

            builder.Property(x => x.PaidAtUtc);
            builder.Property(x => x.CancelledAtUtc);
            builder.Property(x => x.ExpiredAtUtc);
            builder.Property(x => x.CompletedAtUtc);

            builder.Property(x => x.CancellationReason)
                .HasMaxLength(500);

            builder.Property(x => x.LastModificationTime)
                .IsConcurrencyToken();

            builder.HasIndex(x => x.OrderNumber)
                .IsUnique();

            builder.HasIndex(x => x.SourceCartId)
                .IsUnique()
                .HasFilter(
                    $"\"IsDeleted\" = false AND \"Status\" IN " +
                    $"({(int)OrderStatus.Pending}, {(int)OrderStatus.Processing})");

            builder.HasIndex(x => new { x.BuyerUserId, x.CreationTime });
            builder.HasIndex(x => new { x.BuyerUserId, x.Status, x.CreationTime });
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.PaymentStatus);
            builder.HasIndex(x => x.PaidAtUtc);

            builder.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(OrderAggregate.Items))
                ?.SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasMany(x => x.PaymentAttempts)
                .WithOne()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Metadata.FindNavigation(nameof(OrderAggregate.PaymentAttempts))
                ?.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
