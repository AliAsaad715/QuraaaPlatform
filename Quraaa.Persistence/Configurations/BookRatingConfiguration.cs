using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Quraaa.Domain.Catalog;
using Quraaa.Domain.Ratings;
using Quraaa.Domain.User;

namespace Quraaa.Persistence.Configurations
{
    public class BookRatingConfiguration : IEntityTypeConfiguration<BookRatingAggregate>
    {
        public void Configure(EntityTypeBuilder<BookRatingAggregate> builder)
        {
            builder.ToTable("BookRatings", table =>
            {
                table.HasCheckConstraint(
                    "CK_BookRatings_RatingValue_Range",
                    "\"RatingValue\" >= 1 AND \"RatingValue\" <= 5");
            });

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id)
                   .ValueGeneratedNever();

            builder.Property(r => r.UserId)
                   .IsRequired();

            builder.Property(r => r.BookId)
                   .IsRequired();

            builder.Property(r => r.RatingValue)
                   .IsRequired();

            builder.HasIndex(r => r.BookId);
            builder.HasIndex(r => r.UserId);
            builder.HasIndex(r => r.RatingValue);
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
