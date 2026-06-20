using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Category;

namespace Quraaa.Persistence.Configurations
{
    public class BookConfiguration : IEntityTypeConfiguration<BookAggregate>
    {
        public void Configure(EntityTypeBuilder<BookAggregate> builder)
        {
            builder.ToTable("Books");

            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id)
                   .ValueGeneratedNever();

            builder.Property(b => b.Title)
                   .HasMaxLength(250)
                   .IsRequired();

            builder.Property(b => b.Author)
                   .HasMaxLength(150)
                   .IsRequired();

            builder.Property(b => b.Description)
                   .HasMaxLength(2000)
                   .IsRequired();

            builder.Property(b => b.CoverImageUrl)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(b => b.Language)
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(b => b.Isbn)
                   .HasMaxLength(20)
                   .IsRequired(false);

            builder.Property(b => b.CategoryId)
                   .IsRequired();

            builder.HasOne<CategoryAggregate>()
                   .WithMany()
                   .HasForeignKey(b => b.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(b => b.CategoryId);
            builder.HasIndex(b => b.Title);
            builder.HasIndex(b => b.Isbn).IsUnique();
            builder.HasIndex(b => new { b.Title, b.Author, b.Language })
                   .IsUnique();
        }
    }
}