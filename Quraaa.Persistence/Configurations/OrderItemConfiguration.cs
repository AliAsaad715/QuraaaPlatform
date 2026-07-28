using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Orders.Entities;

namespace Quraaa.Persistence.Configurations
{
    public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems", table =>
            {
                table.HasCheckConstraint(
                    "CK_OrderItems_Quantity_Positive",
                    "\"Quantity\" > 0");

                table.HasCheckConstraint(
                    "CK_OrderItems_UnitPriceMinor_Positive",
                    "\"UnitPriceMinor\" > 0");

                table.HasCheckConstraint(
                    "CK_OrderItems_TotalPriceMinor_Positive",
                    "\"TotalPriceMinor\" > 0");

                table.HasCheckConstraint(
                    "CK_OrderItems_TotalPriceMinor_Consistent",
                    "\"TotalPriceMinor\" = \"UnitPriceMinor\" * \"Quantity\"");

                table.HasCheckConstraint(
                    "CK_OrderItems_SellerType_Valid",
                    "\"SellerType\" IN (1, 2)");

                table.HasCheckConstraint(
                    "CK_OrderItems_FormatSnapshot_Valid",
                    "(\"Format\" = 1 AND \"DigitalAssetUrlSnapshot\" IS NOT NULL " +
                    "AND \"ConditionSnapshot\" IS NULL) OR " +
                    "(\"Format\" = 2 AND \"DigitalAssetUrlSnapshot\" IS NULL " +
                    "AND \"ConditionSnapshot\" IS NOT NULL)");
            });

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.OrderId).IsRequired();
            builder.Property(x => x.BookId).IsRequired();
            builder.Property(x => x.ListingId).IsRequired();
            builder.Property(x => x.SellerId).IsRequired();

            builder.Property(x => x.SellerType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Format)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.ConditionSnapshot)
                .HasConversion<int>();

            builder.Property(x => x.FulfillmentStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.BookTitleSnapshot)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.BookAuthorSnapshot)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.BookCoverImageUrlSnapshot)
                .HasMaxLength(500);

            builder.Property(x => x.DigitalAssetUrlSnapshot)
                .HasMaxLength(2048);

            builder.Property(x => x.SellerNameSnapshot)
                .HasMaxLength(250)
                .IsRequired();

            builder.Property(x => x.Quantity).IsRequired();
            builder.Property(x => x.UnitPriceMinor).IsRequired();
            builder.Property(x => x.TotalPriceMinor).IsRequired();

            builder.HasIndex(x => new { x.OrderId, x.ListingId })
                .IsUnique();

            builder.HasIndex(x => x.BookId);
            builder.HasIndex(x => x.ListingId);
            builder.HasIndex(x => x.SellerId);
            builder.HasIndex(x => new { x.SellerType, x.SellerId });
            builder.HasIndex(x => new { x.SellerId, x.FulfillmentStatus });
        }
    }
}
