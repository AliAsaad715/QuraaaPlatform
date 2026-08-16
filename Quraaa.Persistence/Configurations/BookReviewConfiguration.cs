using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Reviews;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
    public class BookReviewConfiguration : IEntityTypeConfiguration<BookReviewAggregate>
    {
        public void Configure(EntityTypeBuilder<BookReviewAggregate> builder)
        {
            builder.ToTable("BookReviews", table =>
            {
                table.HasCheckConstraint(
                    "CK_BookReviews_Score_Range",
                    "\"Score\" >= 1 AND \"Score\" <= 5");
            });

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id)
                   .ValueGeneratedNever();

            builder.Property(r => r.UserId)
                   .IsRequired();

            builder.Property(r => r.BookId)
                   .IsRequired();

            builder.Property(r => r.Score)
                   .IsRequired();

            builder.Property(r => r.Content)
                   .HasMaxLength(1000)
                   .IsRequired();

            builder.HasIndex(r => r.BookId);
            builder.HasIndex(r => r.UserId);
            builder.HasIndex(r => r.Score);
            builder.HasIndex(r => r.CreationTime);
            builder.HasIndex(r => new { r.UserId, r.BookId })
                   .IsUnique();

            builder.HasOne<UserAggregate>()
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<BookAggregate>()
                   .WithMany()
                   .HasForeignKey(r => r.BookId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
