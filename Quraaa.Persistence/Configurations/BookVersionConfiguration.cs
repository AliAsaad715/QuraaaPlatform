using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;

namespace Quraaa.Persistence.Configurations
{
    public sealed class BookVersionConfiguration : IEntityTypeConfiguration<BookVersion>
    {
        public void Configure(EntityTypeBuilder<BookVersion> builder)
        {
            builder.ToTable("BookVersions", table =>
            {
                table.HasCheckConstraint(
                    "CK_BookVersions_VersionNumber_Positive",
                    "\"VersionNumber\" > 0");

                // Only a revert records the version it restored.
                table.HasCheckConstraint(
                    "CK_BookVersions_RevertedFrom_Consistent",
                    "(\"Reason\" = 3 AND \"RevertedFromVersionNumber\" IS NOT NULL) OR " +
                    "(\"Reason\" <> 3 AND \"RevertedFromVersionNumber\" IS NULL)");
            });

            builder.HasKey(version => version.Id);
            builder.Property(version => version.Id).ValueGeneratedNever();

            builder.Property(version => version.BookId).IsRequired();
            builder.Property(version => version.VersionNumber).IsRequired();

            builder.Property(version => version.Reason)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(version => version.RevertedFromVersionNumber);
            builder.Property(version => version.ChangedByUserId);

            builder.Property(version => version.Title)
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(version => version.AuthorId);

            builder.Property(version => version.Description)
                .IsRequired();

            builder.Property(version => version.CoverImageUrl)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(version => version.CategoryId);

            builder.Property(version => version.Language)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(version => version.Isbn)
                .HasMaxLength(20);

            // A book has exactly one row per version number.
            builder.HasIndex(version => new { version.BookId, version.VersionNumber })
                .IsUnique();

            builder.HasOne<BookAggregate>()
                .WithMany()
                .HasForeignKey(version => version.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
