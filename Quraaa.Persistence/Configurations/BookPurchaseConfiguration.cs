using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.Purchases;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
    public class BookPurchaseConfiguration : IEntityTypeConfiguration<BookPurchaseAggregate>
    {
        public void Configure(EntityTypeBuilder<BookPurchaseAggregate> builder)
        {
            builder.ToTable("BookPurchases", table =>
            {
                table.HasCheckConstraint("CK_BookPurchases_Quantity_Positive", "\"Quantity\" > 0");
                table.HasCheckConstraint("CK_BookPurchases_UnitPrice_NonNegative", "\"UnitPrice\" >= 0");
                table.HasCheckConstraint(
                    "CK_BookPurchases_OrderReferences_Paired",
                    "(\"OrderId\" IS NULL AND \"OrderItemId\" IS NULL) OR " +
                    "(\"OrderId\" IS NOT NULL AND \"OrderItemId\" IS NOT NULL)");
            });

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                   .ValueGeneratedNever();

            builder.Property(p => p.UserId)
                   .IsRequired();

            builder.Property(p => p.BookId)
                   .IsRequired();

            builder.Property(p => p.ListingId)
                   .IsRequired();

            builder.Property(p => p.OrderId);
            builder.Property(p => p.OrderItemId);

            builder.Property(p => p.Quantity)
                   .IsRequired();

            builder.Property(p => p.UnitPrice)
                   .HasColumnType("decimal(10,2)")
                   .IsRequired();

            builder.Property(p => p.PurchasedDigitalAssetUrl)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Ignore(p => p.TotalPrice);

            builder.HasIndex(p => p.BookId);
            builder.HasIndex(p => p.UserId);
            builder.HasIndex(p => p.ListingId);
            builder.HasIndex(p => p.OrderId);
            builder.HasIndex(p => p.OrderItemId)
                   .IsUnique()
                   .HasFilter("\"OrderItemId\" IS NOT NULL");
            builder.HasIndex(p => p.CreationTime);
            builder.HasIndex(p => new { p.BookId, p.CreationTime });

            // Drives the "is this path still referenced" lookup the file retention
            // worker runs before hard-deleting a candidate.
            builder.HasIndex(p => p.PurchasedDigitalAssetUrl)
                   .HasFilter("\"PurchasedDigitalAssetUrl\" IS NOT NULL");

            builder.HasOne<UserAggregate>()
                   .WithMany()
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<BookAggregate>()
                   .WithMany()
                   .HasForeignKey(p => p.BookId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ListingAggregate>()
                   .WithMany()
                   .HasForeignKey(p => p.ListingId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
