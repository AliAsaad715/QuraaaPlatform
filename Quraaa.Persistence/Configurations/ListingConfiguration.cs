using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Library;
using Quraaa.Domain.Marketplace;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
    public class ListingConfiguration : IEntityTypeConfiguration<ListingAggregate>
    {
        public void Configure(EntityTypeBuilder<ListingAggregate> builder)
        {
            builder.ToTable("Listings", t => t.HasCheckConstraint(
                "CK_Listing_ExactlyOneSeller",
                "(\"LibraryId\" IS NOT NULL AND \"UserId\" IS NULL) OR (\"LibraryId\" IS NULL AND \"UserId\" IS NOT NULL)"));

            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id)
                   .ValueGeneratedNever();

            builder.Property(l => l.BookId)
                   .IsRequired();

            builder.Property(l => l.SellerType)
                   .IsRequired();

            builder.Property(l => l.Format)
                   .IsRequired();

            builder.Property(l => l.Status)
                   .IsRequired();

            // Adjust precision to match whatever convention you use for
            // money elsewhere - this is just a sensible default.
            builder.Property(l => l.Price)
                   .HasColumnType("decimal(10,2)")
                   .IsRequired();

            // Nullable by design: Condition/Stock only apply to physical
            // listings, CustomDigitalAssetUrl only to digital ones - enforced in
            // ListingAggregate's factory methods, not here.
            builder.Property(l => l.Condition);

            builder.Property(l => l.CustomDigitalAssetUrl)
                   .HasMaxLength(500);

            builder.Property(l => l.Stock);
            builder.Property(l => l.LastModificationTime)
                   .IsConcurrencyToken();

            builder.HasOne<BookAggregate>()
                   .WithMany()
                   .HasForeignKey(l => l.BookId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<LibraryAggregate>()
                   .WithMany()
                   .HasForeignKey(l => l.LibraryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<UserAggregate>()
                   .WithMany()
                   .HasForeignKey(l => l.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(l => l.BookId);
            builder.HasIndex(l => l.LibraryId);
            builder.HasIndex(l => l.UserId);
            builder.HasIndex(l => l.Status);

            // Drives the "is this path still referenced" lookup the file retention
            // worker runs before hard-deleting a candidate.
            builder.HasIndex(l => l.CustomDigitalAssetUrl)
                   .HasFilter("\"CustomDigitalAssetUrl\" IS NOT NULL");
        }
    }
}
