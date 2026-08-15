using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Author;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Category;

namespace Quraaa.Persistence.Configurations
{
    public class BookConfiguration : IEntityTypeConfiguration<BookAggregate>
    {
        public void Configure(EntityTypeBuilder<BookAggregate> builder)
        {
            builder.ToTable("Books");

            builder.Property(b => b.CurrentVersionNumber)
                   .IsRequired()
                   .HasDefaultValue(1);

            builder.Property(b => b.ModerationStatus)
                   .HasConversion<int>()
                   .IsRequired()
                   .HasDefaultValue(Quraaa.Domain.Catalog.Enums.BookModerationStatus.Visible);

            builder.Property(b => b.HiddenAtUtc);

            builder.Property(b => b.ModerationNote)
                   .HasMaxLength(BookAggregate.MaxModerationNoteLength);

            builder.HasIndex(b => b.ModerationStatus);

            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id)
                   .ValueGeneratedNever();

            builder.Property(b => b.Title)
                   .HasMaxLength(250)
                   .IsRequired();

            builder.Property(b => b.AuthorId)
                   .IsRequired(false);

            builder.Property(b => b.Description)
                   .HasMaxLength(2000)
                   .IsRequired();

            builder.Property(b => b.CoverImageUrl)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(b => b.Language)
                   .IsRequired();

            builder.Property(b => b.Isbn)
                   .HasMaxLength(20)
                   .IsRequired(false);

            builder.Property(b => b.CanonicalPdfUrl)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(b => b.CanonicalWordDocUrl)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(b => b.CategoryId)
                   .IsRequired(false);

            builder.HasOne<CategoryAggregate>()
                   .WithMany()
                   .HasForeignKey(b => b.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<AuthorAggregate>()
                   .WithMany()
                   .HasForeignKey(b => b.AuthorId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(b => b.CategoryId);

            builder.HasIndex(b => b.AuthorId);

            // Fast path for the broad title-IN lookup used by FindExistingCandidatesAsync.
            builder.HasIndex(b => b.Title);

            // Partial unique index: the WHERE clause keeps the index small and allows
            // multiple rows with Isbn = NULL (ISBN is assigned later, not during bulk upload).
            builder.HasIndex(b => b.Isbn)
                   .IsUnique()
                   .HasFilter("\"Isbn\" IS NOT NULL");

            // The raw-value composite unique index is intentionally ABSENT here.
            // Migration AddCaseInsensitiveBookIndexes replaces it with a functional
            // unique index on (lower("Title"), lower("Author"), lower("Language")),
            // which enforces case-insensitive uniqueness at the database level.
            // Application-side pre-checks use BookTextNormalizer to avoid round-trips
            // on duplicate attempts before they hit the DB constraint.
        }
    }
}